using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CodeAgent.Sandbox.Configuration;

namespace CodeAgent.Sandbox.Services;

public class SandboxCleanupService : BackgroundService
{
    private readonly ILogger<SandboxCleanupService> _logger;
    private readonly ISandboxManager _sandboxManager;
    private readonly SandboxOptions _options;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public SandboxCleanupService(
        ILogger<SandboxCleanupService> logger,
        ISandboxManager sandboxManager,
        IOptions<SandboxOptions> options)
    {
        _logger = logger;
        _sandboxManager = sandboxManager;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sandbox cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSandboxesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sandbox cleanup");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Sandbox cleanup service stopped");
    }

    private async Task CleanupExpiredSandboxesAsync(CancellationToken cancellationToken)
    {
        var sandboxes = await _sandboxManager.ListSandboxesAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var sandbox in sandboxes)
        {
            var age = now - sandbox.CreatedAt;
            
            // Check if sandbox has exceeded timeout
            if (age > _options.ContainerTimeout)
            {
                _logger.LogInformation("Cleaning up expired sandbox {SandboxId} (age: {Age})", 
                    sandbox.Id, age);

                try
                {
                    await _sandboxManager.DestroySandboxAsync(sandbox.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup sandbox {SandboxId}", sandbox.Id);
                }
            }
        }
    }
}