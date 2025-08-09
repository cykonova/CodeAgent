using System.Text.Json;
using CodeAgent.Projects.Interfaces;
using CodeAgent.Projects.Models;
using CodeAgent.Projects.Templates;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Projects.Services;

public class ConfigurationManager : IConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly ITemplateProvider _templateProvider;
    private readonly Dictionary<Guid, ProjectConfiguration> _projectConfigs = new();
    private ProjectConfiguration? _userDefaults = null;
    private readonly ProjectConfiguration _systemDefaults;

    public ConfigurationManager(ILogger<ConfigurationManager> logger, ITemplateProvider templateProvider)
    {
        _logger = logger;
        _templateProvider = templateProvider;
        _systemDefaults = CreateSystemDefaults();
    }

    public ProjectConfiguration GetEffectiveConfiguration(Guid projectId)
    {
        var configs = new List<ProjectConfiguration?>
        {
            _systemDefaults,
            _userDefaults,
            _projectConfigs.GetValueOrDefault(projectId)
        };

        return MergeConfigurations(configs.ToArray());
    }

    public ProjectConfiguration MergeConfigurations(params ProjectConfiguration?[] configurations)
    {
        var result = new ProjectConfiguration();

        foreach (var config in configurations.Where(c => c != null))
        {
            if (config == null) continue;

            result.ProviderId = config.ProviderId ?? result.ProviderId;
            
            if (config.Workflow != null)
            {
                result.Workflow = MergeWorkflowConfig(result.Workflow, config.Workflow);
            }

            if (config.Agents != null)
            {
                result.Agents = MergeAgentConfig(result.Agents, config.Agents);
            }

            if (config.CostLimits != null)
            {
                result.CostLimits = MergeCostConfig(result.CostLimits, config.CostLimits);
            }

            if (config.Sandbox != null)
            {
                result.Sandbox = MergeSandboxConfig(result.Sandbox, config.Sandbox);
            }

            foreach (var kvp in config.CustomSettings)
            {
                result.CustomSettings[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    public ProjectConfiguration ApplyTemplate(ProjectConfiguration baseConfig, string templateName)
    {
        var template = _templateProvider.GetTemplate(templateName);
        if (template == null)
        {
            _logger.LogWarning("Template {TemplateName} not found, using base configuration", templateName);
            return baseConfig;
        }

        return MergeConfigurations(baseConfig, template);
    }

    public ProjectConfiguration ApplyOverrides(ProjectConfiguration config, Dictionary<string, object> overrides)
    {
        var jsonConfig = JsonSerializer.Serialize(config);
        var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfig) ?? new();

        foreach (var kvp in overrides)
        {
            ApplyOverride(configDict, kvp.Key, kvp.Value);
        }

        var updatedJson = JsonSerializer.Serialize(configDict);
        return JsonSerializer.Deserialize<ProjectConfiguration>(updatedJson) ?? config;
    }

    public bool ValidateConfiguration(ProjectConfiguration configuration, out List<string> errors)
    {
        errors = new List<string>();

        if (configuration.Workflow?.Stages == null || configuration.Workflow.Stages.Count == 0)
        {
            errors.Add("Workflow must have at least one stage");
        }

        foreach (var stage in configuration.Workflow?.Stages ?? new())
        {
            if (string.IsNullOrEmpty(stage.Name))
            {
                errors.Add("All workflow stages must have a name");
            }

            if (string.IsNullOrEmpty(stage.AgentType))
            {
                errors.Add($"Stage {stage.Name} must have an agent type");
            }
        }

        if (configuration.CostLimits?.EnableCostTracking == true)
        {
            if (configuration.CostLimits.MaxCostPerRun == null && 
                configuration.CostLimits.MaxCostPerDay == null && 
                configuration.CostLimits.MaxCostPerMonth == null)
            {
                errors.Add("At least one cost limit must be set when cost tracking is enabled");
            }
        }

        if (configuration.Agents?.MaxConcurrentAgents < 1)
        {
            errors.Add("MaxConcurrentAgents must be at least 1");
        }

        return errors.Count == 0;
    }

    public ProjectConfiguration GetDefaultConfiguration()
    {
        return MergeConfigurations(_systemDefaults, _userDefaults);
    }

    public ProjectConfiguration GetSystemDefaults()
    {
        return _systemDefaults;
    }

    public ProjectConfiguration GetUserDefaults(string? userId = null)
    {
        return _userDefaults ?? _systemDefaults;
    }

    public async Task<ProjectConfiguration> LoadConfigurationAsync(string configPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await File.ReadAllTextAsync(configPath, cancellationToken);
            return JsonSerializer.Deserialize<ProjectConfiguration>(json) ?? new ProjectConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {ConfigPath}", configPath);
            return new ProjectConfiguration();
        }
    }

    public async Task SaveConfigurationAsync(ProjectConfiguration configuration, string configPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(configPath, json, cancellationToken);
            _logger.LogInformation("Saved configuration to {ConfigPath}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {ConfigPath}", configPath);
            throw;
        }
    }

    private ProjectConfiguration CreateSystemDefaults()
    {
        return new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration
            {
                Name = "default",
                Stages = new List<WorkflowStage>
                {
                    new() { Name = "Plan", AgentType = "planner" },
                    new() { Name = "Implement", AgentType = "developer" },
                    new() { Name = "Review", AgentType = "reviewer" }
                },
                AllowParallel = false
            },
            Agents = new AgentConfiguration
            {
                DefaultModel = "gpt-4",
                MaxConcurrentAgents = 3
            },
            CostLimits = new CostConfiguration
            {
                EnableCostTracking = true,
                AlertLevel = CostAlertLevel.Warning,
                MaxCostPerRun = 10.00m,
                MaxCostPerDay = 100.00m,
                MaxCostPerMonth = 1000.00m
            },
            Sandbox = new SandboxConfiguration
            {
                SecurityLevel = SecurityLevel.Container,
                Resources = new ResourceLimits
                {
                    Memory = "2G",
                    Cpu = "2",
                    Disk = "10G",
                    TimeoutSeconds = 3600
                }
            }
        };
    }

    private WorkflowConfiguration MergeWorkflowConfig(WorkflowConfiguration target, WorkflowConfiguration source)
    {
        target.Name = source.Name ?? target.Name;
        target.AllowParallel = source.AllowParallel;
        
        if (source.Stages?.Count > 0)
        {
            target.Stages = source.Stages;
        }

        foreach (var kvp in source.Options)
        {
            target.Options[kvp.Key] = kvp.Value;
        }

        return target;
    }

    private AgentConfiguration MergeAgentConfig(AgentConfiguration target, AgentConfiguration source)
    {
        target.DefaultModel = source.DefaultModel ?? target.DefaultModel;
        target.MaxConcurrentAgents = source.MaxConcurrentAgents > 0 ? source.MaxConcurrentAgents : target.MaxConcurrentAgents;

        foreach (var kvp in source.AgentSettings)
        {
            target.AgentSettings[kvp.Key] = kvp.Value;
        }

        return target;
    }

    private CostConfiguration MergeCostConfig(CostConfiguration target, CostConfiguration source)
    {
        target.MaxCostPerRun = source.MaxCostPerRun ?? target.MaxCostPerRun;
        target.MaxCostPerDay = source.MaxCostPerDay ?? target.MaxCostPerDay;
        target.MaxCostPerMonth = source.MaxCostPerMonth ?? target.MaxCostPerMonth;
        target.MaxTokensPerRun = source.MaxTokensPerRun ?? target.MaxTokensPerRun;
        target.EnableCostTracking = source.EnableCostTracking;
        target.AlertLevel = source.AlertLevel;

        return target;
    }

    private SandboxConfiguration MergeSandboxConfig(SandboxConfiguration target, SandboxConfiguration source)
    {
        target.SecurityLevel = source.SecurityLevel;
        target.DockerImage = source.DockerImage ?? target.DockerImage;
        
        if (source.AllowedCommands?.Count > 0)
        {
            target.AllowedCommands = source.AllowedCommands;
        }

        if (source.BlockedCommands?.Count > 0)
        {
            target.BlockedCommands = source.BlockedCommands;
        }

        foreach (var kvp in source.EnvironmentVariables)
        {
            target.EnvironmentVariables[kvp.Key] = kvp.Value;
        }

        if (source.Resources != null)
        {
            target.Resources = source.Resources;
        }

        return target;
    }

    private void ApplyOverride(Dictionary<string, object> target, string path, object value)
    {
        var parts = path.Split('.');
        var current = target;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!current.ContainsKey(parts[i]))
            {
                current[parts[i]] = new Dictionary<string, object>();
            }

            if (current[parts[i]] is JsonElement element)
            {
                current[parts[i]] = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText()) 
                    ?? new Dictionary<string, object>();
            }

            current = (Dictionary<string, object>)current[parts[i]];
        }

        current[parts[^1]] = value;
    }
}