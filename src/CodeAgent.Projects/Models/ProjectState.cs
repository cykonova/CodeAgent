namespace CodeAgent.Projects.Models;

public class ProjectState
{
    public ProjectStatus Status { get; set; } = ProjectStatus.Idle;
    public string? CurrentStage { get; set; }
    public DateTime? LastRunAt { get; set; }
    public TimeSpan? LastRunDuration { get; set; }
    public List<ProjectRun> RunHistory { get; set; } = new();
    public CostSummary CostSummary { get; set; } = new();
    public Dictionary<string, object> RuntimeData { get; set; } = new();
}

public enum ProjectStatus
{
    Idle,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

public class ProjectRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ProjectStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public List<StageResult> StageResults { get; set; } = new();
    public RunCost Cost { get; set; } = new();
}

public class StageResult
{
    public string StageName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public StageStatus Status { get; set; }
    public string? AgentId { get; set; }
    public Dictionary<string, object> Output { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public enum StageStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

public class CostSummary
{
    public decimal TotalCost { get; set; }
    public int TotalTokens { get; set; }
    public decimal TodayCost { get; set; }
    public int TodayTokens { get; set; }
    public decimal MonthCost { get; set; }
    public int MonthTokens { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class RunCost
{
    public decimal TotalCost { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public Dictionary<string, ProviderCost> ProviderCosts { get; set; } = new();
}

public class ProviderCost
{
    public string ProviderId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
}