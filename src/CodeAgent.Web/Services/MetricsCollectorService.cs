using CodeAgent.Domain.Interfaces;

namespace CodeAgent.Web.Services;

public class MetricsCollectorService : BackgroundService
{
    private readonly ITelemetryService _telemetryService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<MetricsCollectorService> _logger;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromMinutes(1);

    public MetricsCollectorService(
        ITelemetryService telemetryService,
        IAnalyticsService analyticsService,
        ILogger<MetricsCollectorService> logger)
    {
        _telemetryService = telemetryService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics collector service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Collect system metrics
                await _telemetryService.CollectMetricsAsync(stoppingToken);

                // Get current metrics
                var metrics = await _telemetryService.GetCurrentMetricsAsync(stoppingToken);

                // Track in analytics
                await _analyticsService.TrackMetricAsync("system.memory", metrics.MemoryUsedBytes, 
                    new Dictionary<string, string> { ["type"] = "bytes" }, stoppingToken);
                
                await _analyticsService.TrackMetricAsync("system.cpu", metrics.CpuUsagePercent,
                    new Dictionary<string, string> { ["type"] = "percent" }, stoppingToken);
                
                await _analyticsService.TrackMetricAsync("system.connections", metrics.ActiveConnections,
                    new Dictionary<string, string> { ["type"] = "count" }, stoppingToken);

                // Check health status
                var health = await _telemetryService.GetHealthStatusAsync(stoppingToken);
                if (!health.IsHealthy)
                {
                    _logger.LogWarning("System health check failed: {Status}", health.OverallStatus);
                    await _analyticsService.TrackEventAsync("health.check.failed",
                        new Dictionary<string, object> { ["status"] = health.OverallStatus },
                        stoppingToken);
                }

                await Task.Delay(_collectionInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting metrics");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Metrics collector service stopped");
    }
}