using CodeAgent.Providers.Models;

namespace CodeAgent.Agents.Contracts;

public interface IAgent
{
    string AgentId { get; }
    string Name { get; }
    AgentType Type { get; }
    AgentCapabilities Capabilities { get; }
    
    Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<bool> InitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}