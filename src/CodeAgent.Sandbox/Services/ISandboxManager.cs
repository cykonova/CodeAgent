using CodeAgent.Sandbox.Models;

namespace CodeAgent.Sandbox.Services;

public interface ISandboxManager
{
    Task<SandboxInstance> CreateSandboxAsync(SandboxCreateRequest request, CancellationToken cancellationToken = default);
    Task<SandboxInstance> GetSandboxAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SandboxInstance>> ListSandboxesAsync(CancellationToken cancellationToken = default);
    Task StartSandboxAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task StopSandboxAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task DestroySandboxAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task<SandboxExecutionResult> ExecuteCommandAsync(string sandboxId, string command, CancellationToken cancellationToken = default);
}