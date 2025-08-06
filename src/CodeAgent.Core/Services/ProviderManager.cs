using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Core.Services;

public class ProviderManager : IProviderManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProviderManager> _logger;
    private readonly Dictionary<string, ILLMProvider> _providers = new();
    private string? _currentProviderName;

    public ILLMProvider? CurrentProvider => 
        _currentProviderName != null && _providers.TryGetValue(_currentProviderName, out var provider) 
            ? provider 
            : null;

    public string? CurrentProviderName => _currentProviderName;

    public ProviderManager(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ProviderManager> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public void RegisterProvider(string name, ILLMProvider provider)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Provider name cannot be empty", nameof(name));
        
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        _providers[name] = provider;
        _logger.LogInformation("Registered provider: {ProviderName}", name);
        
        // Set as current if it's the first provider
        if (_currentProviderName == null)
        {
            _currentProviderName = name;
        }
    }

    public async Task<bool> SwitchProviderAsync(string name)
    {
        if (!_providers.ContainsKey(name))
        {
            _logger.LogWarning("Provider not found: {ProviderName}", name);
            return false;
        }

        // Test the provider before switching
        var isAvailable = await TestProviderAsync(name);
        if (!isAvailable)
        {
            _logger.LogWarning("Provider {ProviderName} is not available", name);
            return false;
        }

        _currentProviderName = name;
        _logger.LogInformation("Switched to provider: {ProviderName}", name);
        return true;
    }

    public IReadOnlyList<string> GetAvailableProviders()
    {
        return _providers.Keys.ToList();
    }

    public async Task<bool> TestProviderAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_providers.TryGetValue(name, out var provider))
        {
            return false;
        }

        try
        {
            // Send a simple test message
            var request = new ChatRequest
            {
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "Hello, please respond with 'OK' if you're working." }
                },
                MaxTokens = 10
            };
            
            var response = await provider.SendMessageAsync(request, cancellationToken);
            return !string.IsNullOrEmpty(response?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test provider {ProviderName}", name);
            return false;
        }
    }

    public ProviderCapabilities? GetProviderCapabilities(string name)
    {
        if (!_providers.TryGetValue(name, out var provider))
        {
            return null;
        }

        // Build capabilities based on provider type
        var capabilities = new ProviderCapabilities
        {
            Name = name,
            SupportsStreaming = true, // Most modern providers support streaming
            MaxTokens = 4096, // Default max tokens
            MaxContextLength = 128000 // Default context length
        };

        // Provider-specific capabilities
        var providerType = provider.GetType().Name;
        
        switch (providerType)
        {
            case "OpenAIProvider":
                capabilities.SupportsFunctionCalling = true;
                capabilities.SupportsVision = true;
                capabilities.SupportsEmbeddings = true;
                capabilities.AvailableModels = new List<string> 
                { 
                    "gpt-4-turbo-preview", 
                    "gpt-4", 
                    "gpt-3.5-turbo" 
                };
                break;
                
            case "ClaudeProvider":
                capabilities.SupportsVision = true;
                capabilities.MaxTokens = 8192;
                capabilities.MaxContextLength = 200000;
                capabilities.AvailableModels = new List<string> 
                { 
                    "claude-3-opus-20240229",
                    "claude-3-sonnet-20240229",
                    "claude-3-haiku-20240307"
                };
                break;
                
            case "OllamaProvider":
                capabilities.AvailableModels = new List<string> 
                { 
                    "llama2", 
                    "codellama", 
                    "mistral" 
                };
                break;
        }

        return capabilities;
    }

    public async Task LoadProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providersSection = _configuration.GetSection("Providers");
        
        foreach (var providerConfig in providersSection.GetChildren())
        {
            var providerName = providerConfig.Key;
            var providerType = providerConfig["Type"];
            var enabled = providerConfig["Enabled"] != "false";
            
            if (!enabled)
            {
                _logger.LogDebug("Provider {ProviderName} is disabled", providerName);
                continue;
            }

            try
            {
                ILLMProvider? provider = providerType?.ToLower() switch
                {
                    "openai" => _serviceProvider.GetService<ILLMProvider>(),
                    "claude" => _serviceProvider.GetService<ILLMProvider>(),
                    "ollama" => _serviceProvider.GetService<ILLMProvider>(),
                    _ => null
                };

                if (provider != null)
                {
                    RegisterProvider(providerName, provider);
                }
                else
                {
                    _logger.LogWarning("Unknown provider type: {ProviderType}", providerType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load provider {ProviderName}", providerName);
            }
        }

        // Set default provider if configured
        var defaultProvider = _configuration["DefaultProvider"];
        if (!string.IsNullOrEmpty(defaultProvider) && _providers.ContainsKey(defaultProvider))
        {
            await SwitchProviderAsync(defaultProvider);
        }
    }
}