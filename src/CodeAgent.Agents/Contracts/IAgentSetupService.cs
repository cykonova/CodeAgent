using CodeAgent.Agents.Contracts;

namespace CodeAgent.Agents.Services;

public interface IAgentSetupService
{
    Task<List<IAgent>> SetupAgentsAsync(CancellationToken cancellationToken = default);
}