using CodeAgent.Agents.Contracts;

namespace CodeAgent.Agents.Services;

public interface IAgentOrchestrator
{
    Task<bool>                 InitializeAsync(CancellationToken cancellationToken = default);
    Task<OrchestratorResponse> ProcessCommandAsync(OrchestratorRequest request, CancellationToken cancellationToken = default);
    Task<OrchestratorResponse> ExecuteWorkflowAsync(string workflowId, WorkflowRequest request, CancellationToken cancellationToken = default);
    IAgent?                    GetAgent(string agentId);
    IAgent?                    GetAgentForType(AgentType type);
    IEnumerable<IAgent>        GetAllAgents();
    Task                       ShutdownAsync(CancellationToken cancellationToken = default);
}