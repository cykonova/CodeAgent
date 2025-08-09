namespace CodeAgent.Sandbox.Services;

public interface IAgentRunner
{
    Task<AgentExecutionResult> RunAgentAsync(AgentRunRequest request, CancellationToken cancellationToken = default);
    Task<AgentExecutionStatus> GetExecutionStatusAsync(string executionId, CancellationToken cancellationToken = default);
    Task StopExecutionAsync(string executionId, CancellationToken cancellationToken = default);
}