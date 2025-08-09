using CodeAgent.Agents.Contracts;

namespace CodeAgent.Agents.Services;

public interface IAgentFactory
{
    Task<IAgent?> CreateAgentAsync(AgentType type, CancellationToken cancellationToken = default);
    Task<IAgent?> CreateCustomAgentAsync(string agentClass, CancellationToken cancellationToken = default);
}