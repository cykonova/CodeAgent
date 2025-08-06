using CodeAgent.Domain.Interfaces;

namespace CodeAgent.Web.Services;

public class CleanupService : BackgroundService
{
    private readonly ISessionService _sessionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public CleanupService(
        ISessionService sessionService,
        ICacheService cacheService,
        ILogger<CleanupService> logger)
    {
        _sessionService = sessionService;
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanup(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Cleanup service stopped");
    }

    private async Task PerformCleanup(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cleanup tasks");

        // Clean expired sessions
        var expiredSessions = await CleanExpiredSessions(cancellationToken);
        _logger.LogInformation("Cleaned {Count} expired sessions", expiredSessions);

        // Clear expired cache entries
        await _cacheService.ClearExpiredAsync(cancellationToken);
        _logger.LogInformation("Cleared expired cache entries");

        // Clean temporary files
        var tempFiles = CleanTempFiles();
        _logger.LogInformation("Cleaned {Count} temporary files", tempFiles);

        _logger.LogInformation("Cleanup tasks completed");
    }

    private async Task<int> CleanExpiredSessions(CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.GetAllSessionsAsync(cancellationToken);
        var expiredCount = 0;
        var cutoff = DateTime.UtcNow.AddDays(-30); // Keep sessions for 30 days

        foreach (var session in sessions)
        {
            if (session.LastAccessedAt < cutoff)
            {
                await _sessionService.DeleteSessionAsync(session.Id, cancellationToken);
                expiredCount++;
            }
        }

        return expiredCount;
    }

    private int CleanTempFiles()
    {
        var tempPath = Path.GetTempPath();
        var appTempPrefix = "codeagent_";
        var cutoff = DateTime.UtcNow.AddDays(-1);
        var deletedCount = 0;

        try
        {
            var tempFiles = Directory.GetFiles(tempPath, $"{appTempPrefix}*");
            foreach (var file in tempFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoff)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file {File}", file);
                    }
                }
            }

            var tempDirs = Directory.GetDirectories(tempPath, $"{appTempPrefix}*");
            foreach (var dir in tempDirs)
            {
                var dirInfo = new DirectoryInfo(dir);
                if (dirInfo.LastWriteTimeUtc < cutoff)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp directory {Directory}", dir);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing temp directory");
        }

        return deletedCount;
    }
}