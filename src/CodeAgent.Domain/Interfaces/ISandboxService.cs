using CodeAgent.Domain.Models.Security;

namespace CodeAgent.Domain.Interfaces;

public interface ISandboxService
{
    Task<SandboxEnvironment> CreateSandboxAsync(SandboxConfiguration configuration, CancellationToken cancellationToken = default);
    Task<ExecutionResult> ExecuteInSandboxAsync(string sandboxId, string code, CancellationToken cancellationToken = default);
    Task<bool> DestroySandboxAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task<SandboxStatus> GetSandboxStatusAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task<ResourceUsage> GetResourceUsageAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task<bool> SetResourceLimitsAsync(string sandboxId, ResourceLimits limits, CancellationToken cancellationToken = default);
    Task<IEnumerable<SandboxEnvironment>> GetActiveSandboxesAsync(CancellationToken cancellationToken = default);
    Task<bool> IsolateSandboxNetworkAsync(string sandboxId, NetworkIsolationLevel level, CancellationToken cancellationToken = default);
}