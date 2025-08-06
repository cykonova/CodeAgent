namespace CodeAgent.Domain.Interfaces;

public interface IAnalyticsService
{
    Task TrackEventAsync(string eventName, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task TrackMetricAsync(string metricName, double value, Dictionary<string, string>? dimensions = null, CancellationToken cancellationToken = default);
    Task TrackExceptionAsync(Exception exception, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task TrackPageViewAsync(string pageName, TimeSpan duration, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task<AnalyticsReport> GenerateReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<UsageStatistics> GetUsageStatisticsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TopFeature>> GetTopFeaturesAsync(int count = 10, CancellationToken cancellationToken = default);
}

public class AnalyticsReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalEvents { get; set; }
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> EventCounts { get; set; } = new();
    public Dictionary<string, double> Metrics { get; set; } = new();
    public List<TopFeature> TopFeatures { get; set; } = new();
    public List<UserActivity> MostActiveUsers { get; set; } = new();
}

public class UsageStatistics
{
    public string UserId { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int TotalMessages { get; set; }
    public TimeSpan TotalUsageTime { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public Dictionary<string, int> FeatureUsage { get; set; } = new();
    public List<string> PreferredProviders { get; set; } = new();
}

public class TopFeature
{
    public string Name { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public int UniqueUsers { get; set; }
    public double AverageResponseTime { get; set; }
    public double SatisfactionScore { get; set; }
}

public class UserActivity
{
    public string UserId { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public TimeSpan ActiveTime { get; set; }
    public DateTime LastActivity { get; set; }
}