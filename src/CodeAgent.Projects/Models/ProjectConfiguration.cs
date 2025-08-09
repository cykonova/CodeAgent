namespace CodeAgent.Projects.Models;

public class ProjectConfiguration
{
    public string? ProviderId { get; set; }
    public WorkflowConfiguration Workflow { get; set; } = new();
    public AgentConfiguration Agents { get; set; } = new();
    public CostConfiguration CostLimits { get; set; } = new();
    public SandboxConfiguration Sandbox { get; set; } = new();
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

public class WorkflowConfiguration
{
    public string Name { get; set; } = "default";
    public List<WorkflowStage> Stages { get; set; } = new()
    {
        new() { Name = "Plan", AgentType = "planner" },
        new() { Name = "Implement", AgentType = "developer" },
        new() { Name = "Review", AgentType = "reviewer" }
    };
    public bool AllowParallel { get; set; } = false;
    public Dictionary<string, object> Options { get; set; } = new();
}

public class WorkflowStage
{
    public string Name { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public List<string> DependsOn { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public WorkflowCondition? Condition { get; set; }
}

public class WorkflowCondition
{
    public string Type { get; set; } = string.Empty; // "if", "unless", "when"
    public string Expression { get; set; } = string.Empty;
    public Dictionary<string, object> Variables { get; set; } = new();
}

public class AgentConfiguration
{
    public string DefaultModel { get; set; } = "gpt-4";
    public Dictionary<string, AgentSettings> AgentSettings { get; set; } = new();
    public int MaxConcurrentAgents { get; set; } = 3;
}

public class AgentSettings
{
    public string? Model { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public Dictionary<string, object> CustomParameters { get; set; } = new();
}

public class CostConfiguration
{
    public decimal? MaxCostPerRun { get; set; }
    public decimal? MaxCostPerDay { get; set; }
    public decimal? MaxCostPerMonth { get; set; }
    public int? MaxTokensPerRun { get; set; }
    public bool EnableCostTracking { get; set; } = true;
    public CostAlertLevel AlertLevel { get; set; } = CostAlertLevel.Warning;
}

public enum CostAlertLevel
{
    None,
    Info,
    Warning,
    Error
}

public class SandboxConfiguration
{
    public SecurityLevel SecurityLevel { get; set; } = SecurityLevel.Container;
    public string? DockerImage { get; set; }
    public List<string> AllowedCommands { get; set; } = new();
    public List<string> BlockedCommands { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public ResourceLimits Resources { get; set; } = new();
}

public enum SecurityLevel
{
    None,
    Container,
    VM
}

public class ResourceLimits
{
    public string? Memory { get; set; } = "2G";
    public string? Cpu { get; set; } = "2";
    public string? Disk { get; set; } = "10G";
    public int? TimeoutSeconds { get; set; } = 3600;
}