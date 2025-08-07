using Microsoft.AspNetCore.Mvc;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Core.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger<ConfigurationController> _logger;
    private readonly string _settingsPath;
    private readonly string _yamlSettingsPath;

    public ConfigurationController(
        IConfigurationService configurationService,
        ILLMProviderFactory providerFactory,
        ILogger<ConfigurationController> logger)
    {
        _configurationService = configurationService;
        _providerFactory = providerFactory;
        _logger = logger;
        
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var codeAgentDir = Path.Combine(homeDir, ".codeagent");
        _settingsPath = Path.Combine(codeAgentDir, "settings.json");
        _yamlSettingsPath = Path.Combine(codeAgentDir, "settings.yaml");
    }

    [HttpGet]
    public IActionResult GetConfiguration()
    {
        var config = new
        {
            DefaultProvider = _configurationService.GetValue("DefaultProvider") ?? "openai",
            Providers = new
            {
                OpenAI = new
                {
                    Model = _configurationService.GetValue("OpenAI:Model") ?? "gpt-4",
                    ApiKeySet = !string.IsNullOrEmpty(_configurationService.GetValue("OpenAI:ApiKey"))
                },
                Claude = new
                {
                    Model = _configurationService.GetValue("Claude:Model") ?? "claude-3-sonnet-20240229",
                    ApiKeySet = !string.IsNullOrEmpty(_configurationService.GetValue("Claude:ApiKey"))
                },
                Ollama = new
                {
                    BaseUrl = _configurationService.GetValue("Ollama:BaseUrl") ?? "http://localhost:11434",
                    Model = _configurationService.GetValue("Ollama:Model") ?? "llama2"
                }
            },
            SystemPrompt = _configurationService.GetValue("SystemPrompt")
        };
        
        return Ok(config);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConfiguration([FromBody] ConfigurationUpdateRequest request)
    {
        try
        {
            var settings = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(request.DefaultProvider))
                settings["DefaultProvider"] = request.DefaultProvider;
            
            if (request.OpenAI != null)
            {
                var openAISettings = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(request.OpenAI.ApiKey))
                    openAISettings["ApiKey"] = request.OpenAI.ApiKey;
                if (!string.IsNullOrEmpty(request.OpenAI.Model))
                    openAISettings["Model"] = request.OpenAI.Model;
                if (openAISettings.Any())
                    settings["OpenAI"] = openAISettings;
            }
            
            if (request.Claude != null)
            {
                var claudeSettings = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(request.Claude.ApiKey))
                    claudeSettings["ApiKey"] = request.Claude.ApiKey;
                if (!string.IsNullOrEmpty(request.Claude.Model))
                    claudeSettings["Model"] = request.Claude.Model;
                if (claudeSettings.Any())
                    settings["Claude"] = claudeSettings;
            }
            
            if (request.Ollama != null)
            {
                var ollamaSettings = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(request.Ollama.BaseUrl))
                    ollamaSettings["BaseUrl"] = request.Ollama.BaseUrl;
                if (!string.IsNullOrEmpty(request.Ollama.Model))
                    ollamaSettings["Model"] = request.Ollama.Model;
                if (ollamaSettings.Any())
                    settings["Ollama"] = ollamaSettings;
            }
            
            if (!string.IsNullOrEmpty(request.SystemPrompt))
                settings["SystemPrompt"] = request.SystemPrompt;
            
            // Save based on format preference
            if (request.Format?.ToLower() == "yaml")
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var yaml = serializer.Serialize(settings);
                await System.IO.File.WriteAllTextAsync(_yamlSettingsPath, yaml);
                
                // Delete JSON if it exists
                if (System.IO.File.Exists(_settingsPath))
                    System.IO.File.Delete(_settingsPath);
            }
            else
            {
                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await System.IO.File.WriteAllTextAsync(_settingsPath, json);
                
                // Delete YAML if it exists
                if (System.IO.File.Exists(_yamlSettingsPath))
                    System.IO.File.Delete(_yamlSettingsPath);
            }
            
            _logger.LogInformation("Configuration updated successfully");
            return Ok(new { message = "Configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            return StatusCode(500, new { error = "Failed to update configuration", details = ex.Message });
        }
    }

    [HttpGet("providers")]
    public IActionResult GetAvailableProviders()
    {
        var providers = new[]
        {
            new { Id = "openai", Name = "OpenAI", RequiresApiKey = true },
            new { Id = "claude", Name = "Claude", RequiresApiKey = true },
            new { Id = "ollama", Name = "Ollama", RequiresApiKey = false }
        };
        
        return Ok(providers);
    }

    [HttpPost("provider/switch")]
    public IActionResult SwitchProvider([FromBody] SwitchProviderRequest request)
    {
        try
        {
            _configurationService.SetValue("DefaultProvider", request.ProviderId);
            _logger.LogInformation("Switched to provider: {Provider}", request.ProviderId);
            return Ok(new { message = $"Switched to {request.ProviderId} provider" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch provider");
            return StatusCode(500, new { error = "Failed to switch provider", details = ex.Message });
        }
    }
}

public class ConfigurationUpdateRequest
{
    public string? DefaultProvider { get; set; }
    public ProviderConfig? OpenAI { get; set; }
    public ProviderConfig? Claude { get; set; }
    public OllamaConfig? Ollama { get; set; }
    public string? SystemPrompt { get; set; }
    public string? Format { get; set; } = "json"; // json or yaml
}

public class ProviderConfig
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
}

public class OllamaConfig
{
    public string? BaseUrl { get; set; }
    public string? Model { get; set; }
}

public class SwitchProviderRequest
{
    public string ProviderId { get; set; } = string.Empty;
}