namespace CodeAgent.Domain.Interfaces;

public interface IPerformanceMonitor
{
    /// <summary>
    /// Starts a performance measurement
    /// </summary>
    IDisposable StartMeasurement(string operationName, Dictionary<string, object>? tags = null);
    
    /// <summary>
    /// Records a metric value
    /// </summary>
    void RecordMetric(string name, double value, Dictionary<string, object>? tags = null);
    
    /// <summary>
    /// Records an event
    /// </summary>
    void RecordEvent(string eventName, Dictionary<string, object>? properties = null);
    
    /// <summary>
    /// Gets performance statistics
    /// </summary>
    PerformanceStatistics GetStatistics();
    
    /// <summary>
    /// Resets all metrics
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Exports metrics to a file
    /// </summary>
    Task ExportMetricsAsync(string filePath, CancellationToken cancellationToken = default);
}

public class PerformanceStatistics
{
    public Dictionary<string, OperationStatistics> Operations { get; set; } = new();
    public Dictionary<string, MetricStatistics> Metrics { get; set; } = new();
    public Dictionary<string, int> EventCounts { get; set; } = new();
    public DateTime StartTime { get; set; }
    public TimeSpan TotalRuntime { get; set; }
    public long TotalMemoryUsed { get; set; }
    public double CpuUsagePercent { get; set; }
}

public class OperationStatistics
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double TotalDuration { get; set; }
    public double AverageDuration { get; set; }
    public double MinDuration { get; set; }
    public double MaxDuration { get; set; }
    public double StandardDeviation { get; set; }
}

public class MetricStatistics
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Total { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
}