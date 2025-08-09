using CodeAgent.Projects.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Projects.Templates;

public class TemplateProvider : ITemplateProvider
{
    private readonly ILogger<TemplateProvider> _logger;
    private readonly Dictionary<string, ProjectConfiguration> _templates;

    public TemplateProvider(ILogger<TemplateProvider> logger)
    {
        _logger = logger;
        _templates = new Dictionary<string, ProjectConfiguration>();
        InitializeBuiltInTemplates();
    }

    public ProjectConfiguration? GetTemplate(string templateName)
    {
        if (_templates.TryGetValue(templateName.ToLower(), out var template))
        {
            return template;
        }

        _logger.LogWarning("Template {TemplateName} not found", templateName);
        return null;
    }

    public IEnumerable<string> GetAvailableTemplates()
    {
        return _templates.Keys;
    }

    public void RegisterTemplate(string name, ProjectConfiguration configuration)
    {
        _templates[name.ToLower()] = configuration;
        _logger.LogInformation("Registered template {TemplateName}", name);
    }

    public bool RemoveTemplate(string name)
    {
        var removed = _templates.Remove(name.ToLower());
        if (removed)
        {
            _logger.LogInformation("Removed template {TemplateName}", name);
        }
        return removed;
    }

    private void InitializeBuiltInTemplates()
    {
        RegisterTemplate("standard", CreateStandardTemplate());
        RegisterTemplate("fast", CreateFastTemplate());
        RegisterTemplate("quality", CreateQualityTemplate());
        RegisterTemplate("budget", CreateBudgetTemplate());
    }

    private ProjectConfiguration CreateStandardTemplate()
    {
        return new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration
            {
                Name = "standard",
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
                MaxConcurrentAgents = 3,
                AgentSettings = new Dictionary<string, AgentSettings>
                {
                    ["planner"] = new() { Temperature = 0.7, MaxTokens = 2048 },
                    ["developer"] = new() { Temperature = 0.3, MaxTokens = 4096 },
                    ["reviewer"] = new() { Temperature = 0.5, MaxTokens = 2048 }
                }
            },
            CostLimits = new CostConfiguration
            {
                EnableCostTracking = true,
                AlertLevel = CostAlertLevel.Warning,
                MaxCostPerRun = 10.00m,
                MaxCostPerDay = 100.00m
            },
            Sandbox = new SandboxConfiguration
            {
                SecurityLevel = SecurityLevel.Container,
                Resources = new ResourceLimits
                {
                    Memory = "2G",
                    Cpu = "2",
                    TimeoutSeconds = 3600
                }
            }
        };
    }

    private ProjectConfiguration CreateFastTemplate()
    {
        return new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration
            {
                Name = "fast",
                Stages = new List<WorkflowStage>
                {
                    new() { Name = "QuickPlan", AgentType = "planner" },
                    new() { Name = "Implement", AgentType = "developer" }
                },
                AllowParallel = true
            },
            Agents = new AgentConfiguration
            {
                DefaultModel = "gpt-3.5-turbo",
                MaxConcurrentAgents = 5,
                AgentSettings = new Dictionary<string, AgentSettings>
                {
                    ["planner"] = new() { Temperature = 0.8, MaxTokens = 1024 },
                    ["developer"] = new() { Temperature = 0.5, MaxTokens = 2048 }
                }
            },
            CostLimits = new CostConfiguration
            {
                EnableCostTracking = true,
                AlertLevel = CostAlertLevel.Info,
                MaxCostPerRun = 5.00m
            },
            Sandbox = new SandboxConfiguration
            {
                SecurityLevel = SecurityLevel.None,
                Resources = new ResourceLimits
                {
                    Memory = "1G",
                    Cpu = "1",
                    TimeoutSeconds = 1800
                }
            }
        };
    }

    private ProjectConfiguration CreateQualityTemplate()
    {
        return new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration
            {
                Name = "quality",
                Stages = new List<WorkflowStage>
                {
                    new() { Name = "Analysis", AgentType = "analyzer" },
                    new() { Name = "Plan", AgentType = "planner" },
                    new() { Name = "Implement", AgentType = "developer" },
                    new() { Name = "Test", AgentType = "tester" },
                    new() { Name = "Review", AgentType = "reviewer" },
                    new() { Name = "Documentation", AgentType = "documenter" }
                },
                AllowParallel = false
            },
            Agents = new AgentConfiguration
            {
                DefaultModel = "gpt-4",
                MaxConcurrentAgents = 2,
                AgentSettings = new Dictionary<string, AgentSettings>
                {
                    ["analyzer"] = new() { Temperature = 0.3, MaxTokens = 4096 },
                    ["planner"] = new() { Temperature = 0.5, MaxTokens = 4096 },
                    ["developer"] = new() { Temperature = 0.2, MaxTokens = 8192 },
                    ["tester"] = new() { Temperature = 0.1, MaxTokens = 4096 },
                    ["reviewer"] = new() { Temperature = 0.3, MaxTokens = 4096 },
                    ["documenter"] = new() { Temperature = 0.5, MaxTokens = 2048 }
                }
            },
            CostLimits = new CostConfiguration
            {
                EnableCostTracking = true,
                AlertLevel = CostAlertLevel.Warning,
                MaxCostPerRun = 25.00m,
                MaxCostPerDay = 250.00m
            },
            Sandbox = new SandboxConfiguration
            {
                SecurityLevel = SecurityLevel.Container,
                Resources = new ResourceLimits
                {
                    Memory = "4G",
                    Cpu = "4",
                    TimeoutSeconds = 7200
                }
            }
        };
    }

    private ProjectConfiguration CreateBudgetTemplate()
    {
        return new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration
            {
                Name = "budget",
                Stages = new List<WorkflowStage>
                {
                    new() { Name = "SimplePlan", AgentType = "planner" },
                    new() { Name = "Implement", AgentType = "developer" }
                },
                AllowParallel = false
            },
            Agents = new AgentConfiguration
            {
                DefaultModel = "gpt-3.5-turbo",
                MaxConcurrentAgents = 1,
                AgentSettings = new Dictionary<string, AgentSettings>
                {
                    ["planner"] = new() { Temperature = 0.7, MaxTokens = 512 },
                    ["developer"] = new() { Temperature = 0.5, MaxTokens = 1024 }
                }
            },
            CostLimits = new CostConfiguration
            {
                EnableCostTracking = true,
                AlertLevel = CostAlertLevel.Error,
                MaxCostPerRun = 1.00m,
                MaxCostPerDay = 10.00m,
                MaxCostPerMonth = 100.00m,
                MaxTokensPerRun = 10000
            },
            Sandbox = new SandboxConfiguration
            {
                SecurityLevel = SecurityLevel.None,
                Resources = new ResourceLimits
                {
                    Memory = "512M",
                    Cpu = "0.5",
                    TimeoutSeconds = 900
                }
            }
        };
    }
}