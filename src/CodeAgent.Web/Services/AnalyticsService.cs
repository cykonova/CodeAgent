using CodeAgent.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CodeAgent.Web.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ILogger<AnalyticsService> _logger;
    private readonly ConcurrentBag<AnalyticsEvent> _events = new();
    private readonly ConcurrentDictionary<string, List<double>> _metrics = new();
    private readonly ConcurrentDictionary<string, UsageStatistics> _userStats = new();

    public AnalyticsService(ILogger<AnalyticsService> logger)
    {
        _logger = logger;
    }

    public Task TrackEventAsync(string eventName, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        var analyticsEvent = new AnalyticsEvent
        {
            Name = eventName,
            Properties = properties ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow,
            UserId = GetCurrentUserId()
        };

        _events.Add(analyticsEvent);
        UpdateUserStats(analyticsEvent.UserId, eventName);

        _logger.LogInformation("Tracked event: {EventName}", eventName);
        return Task.CompletedTask;
    }

    public Task TrackMetricAsync(string metricName, double value, Dictionary<string, string>? dimensions = null, CancellationToken cancellationToken = default)
    {
        var key = GenerateMetricKey(metricName, dimensions);
        _metrics.AddOrUpdate(key, 
            new List<double> { value }, 
            (k, list) => { list.Add(value); return list; });

        _logger.LogDebug("Tracked metric: {MetricName} = {Value}", metricName, value);
        return Task.CompletedTask;
    }

    public Task TrackExceptionAsync(Exception exception, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        var errorEvent = new AnalyticsEvent
        {
            Name = "Exception",
            Properties = new Dictionary<string, object>
            {
                ["Type"] = exception.GetType().Name,
                ["Message"] = exception.Message,
                ["StackTrace"] = exception.StackTrace ?? string.Empty
            },
            Timestamp = DateTime.UtcNow,
            UserId = GetCurrentUserId()
        };

        if (properties != null)
        {
            foreach (var prop in properties)
                errorEvent.Properties[prop.Key] = prop.Value;
        }

        _events.Add(errorEvent);
        _logger.LogError(exception, "Tracked exception");
        return Task.CompletedTask;
    }

    public Task TrackPageViewAsync(string pageName, TimeSpan duration, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        var pageViewEvent = new AnalyticsEvent
        {
            Name = "PageView",
            Properties = new Dictionary<string, object>
            {
                ["PageName"] = pageName,
                ["Duration"] = duration.TotalMilliseconds
            },
            Timestamp = DateTime.UtcNow,
            UserId = GetCurrentUserId()
        };

        if (properties != null)
        {
            foreach (var prop in properties)
                pageViewEvent.Properties[prop.Key] = prop.Value;
        }

        _events.Add(pageViewEvent);
        return Task.CompletedTask;
    }

    public Task<AnalyticsReport> GenerateReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var relevantEvents = _events
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .ToList();

        var report = new AnalyticsReport
        {
            PeriodStart = from,
            PeriodEnd = to,
            TotalEvents = relevantEvents.Count,
            UniqueUsers = relevantEvents.Select(e => e.UserId).Distinct().Count(),
            EventCounts = relevantEvents
                .GroupBy(e => e.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopFeatures = GetTopFeaturesFromEvents(relevantEvents),
            MostActiveUsers = GetMostActiveUsers(relevantEvents)
        };

        // Add aggregated metrics
        foreach (var metric in _metrics)
        {
            if (metric.Value.Any())
            {
                report.Metrics[metric.Key + "_avg"] = metric.Value.Average();
                report.Metrics[metric.Key + "_max"] = metric.Value.Max();
                report.Metrics[metric.Key + "_min"] = metric.Value.Min();
            }
        }

        return Task.FromResult(report);
    }

    public Task<UsageStatistics> GetUsageStatisticsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_userStats.TryGetValue(userId, out var stats))
            return Task.FromResult(stats);

        return Task.FromResult(new UsageStatistics { UserId = userId });
    }

    public Task<IEnumerable<TopFeature>> GetTopFeaturesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var features = _events
            .Where(e => e.Name != "Exception" && e.Name != "PageView")
            .GroupBy(e => e.Name)
            .Select(g => new TopFeature
            {
                Name = g.Key,
                UsageCount = g.Count(),
                UniqueUsers = g.Select(e => e.UserId).Distinct().Count(),
                AverageResponseTime = 0, // Would need to track this separately
                SatisfactionScore = 0 // Would need user feedback
            })
            .OrderByDescending(f => f.UsageCount)
            .Take(count);

        return Task.FromResult(features.AsEnumerable());
    }

    private void UpdateUserStats(string userId, string eventName)
    {
        _userStats.AddOrUpdate(userId,
            new UsageStatistics
            {
                UserId = userId,
                TotalSessions = 1,
                TotalMessages = eventName == "SendMessage" ? 1 : 0,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                FeatureUsage = new Dictionary<string, int> { [eventName] = 1 }
            },
            (key, stats) =>
            {
                stats.LastSeen = DateTime.UtcNow;
                if (eventName == "SendMessage")
                    stats.TotalMessages++;
                
                if (stats.FeatureUsage.ContainsKey(eventName))
                    stats.FeatureUsage[eventName]++;
                else
                    stats.FeatureUsage[eventName] = 1;
                
                return stats;
            });
    }

    private List<TopFeature> GetTopFeaturesFromEvents(List<AnalyticsEvent> events)
    {
        return events
            .Where(e => e.Name != "Exception" && e.Name != "PageView")
            .GroupBy(e => e.Name)
            .Select(g => new TopFeature
            {
                Name = g.Key,
                UsageCount = g.Count(),
                UniqueUsers = g.Select(e => e.UserId).Distinct().Count()
            })
            .OrderByDescending(f => f.UsageCount)
            .Take(10)
            .ToList();
    }

    private List<UserActivity> GetMostActiveUsers(List<AnalyticsEvent> events)
    {
        return events
            .GroupBy(e => e.UserId)
            .Select(g => new UserActivity
            {
                UserId = g.Key,
                EventCount = g.Count(),
                LastActivity = g.Max(e => e.Timestamp)
            })
            .OrderByDescending(u => u.EventCount)
            .Take(10)
            .ToList();
    }

    private string GenerateMetricKey(string metricName, Dictionary<string, string>? dimensions)
    {
        if (dimensions == null || !dimensions.Any())
            return metricName;

        var dimensionString = string.Join("_", dimensions.OrderBy(d => d.Key).Select(d => $"{d.Key}:{d.Value}"));
        return $"{metricName}_{dimensionString}";
    }

    private string GetCurrentUserId()
    {
        // In a real implementation, would get from HttpContext or authentication
        return "current-user";
    }

    private class AnalyticsEvent
    {
        public string Name { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}