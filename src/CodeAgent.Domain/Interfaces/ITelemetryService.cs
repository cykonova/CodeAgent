namespace CodeAgent.Domain.Interfaces;

public interface ITelemetryService
{
    Task CollectMetricsAsync(CancellationToken cancellationToken = default);
    Task<SystemMetrics> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);
    Task<PerformanceMetrics> GetPerformanceMetricsAsync(TimeSpan period, CancellationToken cancellationToken = default);
    Task<HealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task AcknowledgeAlertAsync(string alertId, CancellationToken cancellationToken = default);
}

public class SystemMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long MemoryUsedBytes { get; set; }
    public long MemoryAvailableBytes { get; set; }
    public double CpuUsagePercent { get; set; }
    public long DiskUsedBytes { get; set; }
    public long DiskAvailableBytes { get; set; }
    public int ActiveConnections { get; set; }
    public int ActiveSessions { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

public class PerformanceMetrics
{
    public TimeSpan Period { get; set; }
    public double AverageResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, double> EndpointMetrics { get; set; } = new();
}

public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public List<HealthCheck> Checks { get; set; } = new();
    public string OverallStatus { get; set; } = string.Empty;
}

public class HealthCheck
{
    public string Name { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Alert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}