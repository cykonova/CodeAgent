using System.Collections.Concurrent;
using CodeAgent.Providers.Contracts;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Providers.Services;

public class ProviderRegistry : IProviderRegistry
{
    private readonly ConcurrentDictionary<string, ILLMProvider> _providers = new();
    private readonly ConcurrentDictionary<string, ProviderConfiguration> _configurations = new();
    private readonly ILogger<ProviderRegistry> _logger;

    public ProviderRegistry(ILogger<ProviderRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterProvider(ILLMProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        if (_providers.TryAdd(provider.ProviderId, provider))
        {
            _logger.LogInformation("Provider {ProviderId} ({Name}) registered successfully", 
                provider.ProviderId, provider.Name);
        }
        else
        {
            _logger.LogWarning("Provider {ProviderId} is already registered", provider.ProviderId);
        }
    }

    public ILLMProvider? GetProvider(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
            return null;

        _providers.TryGetValue(providerId, out var provider);
        return provider;
    }

    public IEnumerable<ILLMProvider> GetAllProviders()
    {
        return _providers.Values;
    }

    public async Task<ILLMProvider?> GetConnectedProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(providerId);
        if (provider == null)
        {
            _logger.LogWarning("Provider {ProviderId} not found", providerId);
            return null;
        }

        if (!provider.IsConnected)
        {
            if (_configurations.TryGetValue(providerId, out var config))
            {
                await provider.ConnectAsync(config, cancellationToken);
            }
            else
            {
                _logger.LogWarning("No configuration found for provider {ProviderId}", providerId);
                return null;
            }
        }

        return provider;
    }

    public async Task<bool> ConnectProviderAsync(string providerId, ProviderConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var provider = GetProvider(providerId);
        if (provider == null)
        {
            _logger.LogError("Cannot connect: Provider {ProviderId} not registered", providerId);
            return false;
        }

        try
        {
            var isValid = await provider.ValidateConfigurationAsync(configuration, cancellationToken);
            if (!isValid)
            {
                _logger.LogError("Invalid configuration for provider {ProviderId}", providerId);
                return false;
            }

            var connected = await provider.ConnectAsync(configuration, cancellationToken);
            if (connected)
            {
                _configurations.AddOrUpdate(providerId, configuration, (_, _) => configuration);
                _logger.LogInformation("Provider {ProviderId} connected successfully", providerId);
            }
            else
            {
                _logger.LogError("Failed to connect provider {ProviderId}", providerId);
            }

            return connected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting provider {ProviderId}", providerId);
            return false;
        }
    }

    public async Task DisconnectProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(providerId);
        if (provider == null)
        {
            _logger.LogWarning("Cannot disconnect: Provider {ProviderId} not found", providerId);
            return;
        }

        try
        {
            await provider.DisconnectAsync(cancellationToken);
            _configurations.TryRemove(providerId, out _);
            _logger.LogInformation("Provider {ProviderId} disconnected", providerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting provider {ProviderId}", providerId);
        }
    }

    public bool IsProviderRegistered(string providerId)
    {
        return !string.IsNullOrEmpty(providerId) && _providers.ContainsKey(providerId);
    }

    public bool IsProviderConnected(string providerId)
    {
        var provider = GetProvider(providerId);
        return provider?.IsConnected ?? false;
    }
}