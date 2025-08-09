using CodeAgent.Projects.Models;

namespace CodeAgent.Projects.Interfaces;

public interface IWorkflowEngine
{
    Task<ProjectRun> ExecuteWorkflowAsync(Guid projectId, WorkflowConfiguration workflow, CancellationToken cancellationToken = default);
    Task<StageResult> ExecuteStageAsync(Guid projectId, WorkflowStage stage, Dictionary<string, object> context, CancellationToken cancellationToken = default);
    Task<bool> EvaluateConditionAsync(WorkflowCondition condition, Dictionary<string, object> context);
    Task PauseWorkflowAsync(Guid runId, CancellationToken cancellationToken = default);
    Task ResumeWorkflowAsync(Guid runId, CancellationToken cancellationToken = default);
    Task CancelWorkflowAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowStage>> GetExecutionPlanAsync(WorkflowConfiguration workflow, Dictionary<string, object> context);
    Task<bool> ValidateWorkflowAsync(WorkflowConfiguration workflow, out List<string> errors);
    event EventHandler<WorkflowEventArgs> WorkflowStateChanged;
    event EventHandler<StageEventArgs> StageStateChanged;
}

public class WorkflowEventArgs : EventArgs
{
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public ProjectStatus Status { get; set; }
    public string? Message { get; set; }
}

public class StageEventArgs : EventArgs
{
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public StageStatus Status { get; set; }
    public Dictionary<string, object>? Output { get; set; }
    public string? ErrorMessage { get; set; }
}