namespace CodeAgent.Agents.Services;

public interface IWorkflowEngine
{
    Task<WorkflowDefinition?> DetermineWorkflowAsync(OrchestratorRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
    Task                      RegisterWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);
}