using CodeAgent.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CodeAgent.Web.Services;

public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly ConcurrentBag<SystemMetrics> _metricsHistory = new();
    private readonly ConcurrentDictionary<string, Alert> _alerts = new();
    private readonly ConcurrentDictionary<string, long> _requestTimings = new();
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
    }

    public Task CollectMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new SystemMetrics
        {
            MemoryUsedBytes = _currentProcess.WorkingSet64,
            MemoryAvailableBytes = GC.GetTotalMemory(false),
            CpuUsagePercent = CalculateCpuUsage(),
            DiskUsedBytes = GetDiskUsage(),
            ActiveConnections = GetActiveConnections(),
            ActiveSessions = GetActiveSessions()
        };

        _metricsHistory.Add(metrics);
        
        // Keep only last hour of metrics
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var oldMetrics = _metricsHistory.Where(m => m.Timestamp < cutoff).ToList();
        foreach (var old in oldMetrics)
        {
            _metricsHistory.TryTake(out _);
        }

        CheckForAlerts(metrics);
        
        _logger.LogDebug("Collected system metrics: Memory={Memory}MB, CPU={Cpu}%", 
            metrics.MemoryUsedBytes / 1024 / 1024, metrics.CpuUsagePercent);

        return Task.CompletedTask;
    }

    public Task<SystemMetrics> GetCurrentMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = _metricsHistory.OrderByDescending(m => m.Timestamp).FirstOrDefault();
        if (metrics == null)
        {
            // Collect fresh metrics if none available
            CollectMetricsAsync(cancellationToken).Wait(cancellationToken);
            metrics = _metricsHistory.OrderByDescending(m => m.Timestamp).First();
        }

        return Task.FromResult(metrics);
    }

    public Task<PerformanceMetrics> GetPerformanceMetricsAsync(TimeSpan period, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(period);
        var timings = _requestTimings.Values.ToList();
        
        var metrics = new PerformanceMetrics
        {
            Period = period,
            TotalRequests = timings.Count,
            SuccessfulRequests = (int)(timings.Count * 0.95), // Simulated
            FailedRequests = (int)(timings.Count * 0.05), // Simulated
        };

        if (timings.Any())
        {
            timings.Sort();
            metrics.AverageResponseTime = timings.Average();
            metrics.P95ResponseTime = GetPercentile(timings, 0.95);
            metrics.P99ResponseTime = GetPercentile(timings, 0.99);
        }

        metrics.ErrorRate = metrics.TotalRequests > 0 
            ? (double)metrics.FailedRequests / metrics.TotalRequests 
            : 0;

        return Task.FromResult(metrics);
    }

    public async Task<HealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new HealthStatus();
        var checks = new List<HealthCheck>();

        // Database health check
        checks.Add(await CheckDatabaseHealthAsync(cancellationToken));
        
        // Provider health check
        checks.Add(await CheckProviderHealthAsync(cancellationToken));
        
        // Disk space check
        checks.Add(CheckDiskSpace());
        
        // Memory check
        checks.Add(CheckMemory());

        status.Checks = checks;
        status.IsHealthy = checks.All(c => c.IsHealthy);
        status.OverallStatus = status.IsHealthy ? "Healthy" : "Unhealthy";

        return status;
    }

    public Task<IEnumerable<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        var activeAlerts = _alerts.Values
            .Where(a => !a.IsAcknowledged)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .AsEnumerable();

        return Task.FromResult(activeAlerts);
    }

    public Task AcknowledgeAlertAsync(string alertId, CancellationToken cancellationToken = default)
    {
        if (_alerts.TryGetValue(alertId, out var alert))
        {
            alert.IsAcknowledged = true;
            alert.AcknowledgedBy = "current-user"; // Would get from context
            alert.AcknowledgedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Alert {AlertId} acknowledged", alertId);
        }

        return Task.CompletedTask;
    }

    private void CheckForAlerts(SystemMetrics metrics)
    {
        // Check memory usage
        if (metrics.MemoryUsedBytes > 2L * 1024 * 1024 * 1024) // 2GB
        {
            CreateAlert(AlertSeverity.Warning, "High Memory Usage", 
                $"Memory usage is {metrics.MemoryUsedBytes / 1024 / 1024}MB");
        }

        // Check CPU usage
        if (metrics.CpuUsagePercent > 80)
        {
            CreateAlert(AlertSeverity.Warning, "High CPU Usage", 
                $"CPU usage is {metrics.CpuUsagePercent:F1}%");
        }

        // Check disk space
        var diskFreePercent = (double)metrics.DiskAvailableBytes / (metrics.DiskUsedBytes + metrics.DiskAvailableBytes) * 100;
        if (diskFreePercent < 10)
        {
            CreateAlert(AlertSeverity.Error, "Low Disk Space", 
                $"Only {diskFreePercent:F1}% disk space remaining");
        }
    }

    private void CreateAlert(AlertSeverity severity, string title, string message)
    {
        var alertKey = $"{title}_{severity}";
        if (_alerts.ContainsKey(alertKey))
            return; // Alert already exists

        var alert = new Alert
        {
            Severity = severity,
            Title = title,
            Message = message,
            Source = "TelemetryService"
        };

        _alerts[alertKey] = alert;
        _logger.LogWarning("Created alert: {Title} - {Message}", title, message);
    }

    private double CalculateCpuUsage()
    {
        try
        {
            return _currentProcess.TotalProcessorTime.TotalMilliseconds / 
                   Environment.ProcessorCount / 
                   Environment.TickCount * 100;
        }
        catch
        {
            return 0;
        }
    }

    private long GetDiskUsage()
    {
        try
        {
            var drive = new DriveInfo(Directory.GetCurrentDirectory());
            return drive.TotalSize - drive.AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }

    private int GetActiveConnections()
    {
        // Would get from SignalR or HTTP connection tracking
        return Random.Shared.Next(1, 20);
    }

    private int GetActiveSessions()
    {
        // Would get from session service
        return Random.Shared.Next(1, 10);
    }

    private double GetPercentile(List<long> sortedValues, double percentile)
    {
        if (!sortedValues.Any())
            return 0;

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
        return sortedValues[index];
    }

    private async Task<HealthCheck> CheckDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Simulate database check
            await Task.Delay(10, cancellationToken);
            return new HealthCheck
            {
                Name = "Database",
                IsHealthy = true,
                Status = "Connected",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new HealthCheck
            {
                Name = "Database",
                IsHealthy = false,
                Status = "Disconnected",
                ResponseTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<HealthCheck> CheckProviderHealthAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Simulate provider check
            await Task.Delay(5, cancellationToken);
            return new HealthCheck
            {
                Name = "LLM Provider",
                IsHealthy = true,
                Status = "Available",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new HealthCheck
            {
                Name = "LLM Provider",
                IsHealthy = false,
                Status = "Unavailable",
                ResponseTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    private HealthCheck CheckDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Directory.GetCurrentDirectory());
            var freePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
            
            return new HealthCheck
            {
                Name = "Disk Space",
                IsHealthy = freePercent > 10,
                Status = $"{freePercent:F1}% free",
                ResponseTime = TimeSpan.Zero
            };
        }
        catch (Exception ex)
        {
            return new HealthCheck
            {
                Name = "Disk Space",
                IsHealthy = false,
                Status = "Unknown",
                ResponseTime = TimeSpan.Zero,
                ErrorMessage = ex.Message
            };
        }
    }

    private HealthCheck CheckMemory()
    {
        var memoryUsed = _currentProcess.WorkingSet64;
        var memoryLimit = 4L * 1024 * 1024 * 1024; // 4GB limit
        var isHealthy = memoryUsed < memoryLimit * 0.9; // Healthy if under 90%
        
        return new HealthCheck
        {
            Name = "Memory",
            IsHealthy = isHealthy,
            Status = $"{memoryUsed / 1024 / 1024}MB used",
            ResponseTime = TimeSpan.Zero
        };
    }
}