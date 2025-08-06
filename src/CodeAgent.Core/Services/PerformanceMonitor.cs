using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace CodeAgent.Core.Services;

public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly ConcurrentDictionary<string, List<double>> _operationDurations = new();
    private readonly ConcurrentDictionary<string, List<double>> _metricValues = new();
    private readonly ConcurrentDictionary<string, int> _eventCounts = new();
    private readonly DateTime _startTime;
    private readonly Stopwatch _totalRuntime;
    private readonly Process _currentProcess;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _startTime = DateTime.UtcNow;
        _totalRuntime = Stopwatch.StartNew();
        _currentProcess = Process.GetCurrentProcess();
    }

    public IDisposable StartMeasurement(string operationName, Dictionary<string, object>? tags = null)
    {
        return new OperationMeasurement(this, operationName, tags);
    }

    public void RecordMetric(string name, double value, Dictionary<string, object>? tags = null)
    {
        _metricValues.AddOrUpdate(name,
            new List<double> { value },
            (key, list) =>
            {
                list.Add(value);
                return list;
            });

        _logger.LogDebug("Recorded metric {MetricName}: {Value}", name, value);
    }

    public void RecordEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        _eventCounts.AddOrUpdate(eventName, 1, (key, count) => count + 1);
        
        if (properties != null)
        {
            _logger.LogDebug("Event {EventName} recorded with properties: {Properties}", 
                eventName, JsonSerializer.Serialize(properties));
        }
        else
        {
            _logger.LogDebug("Event {EventName} recorded", eventName);
        }
    }

    public PerformanceStatistics GetStatistics()
    {
        var stats = new PerformanceStatistics
        {
            StartTime = _startTime,
            TotalRuntime = _totalRuntime.Elapsed,
            TotalMemoryUsed = GC.GetTotalMemory(false),
            CpuUsagePercent = GetCpuUsage()
        };

        // Calculate operation statistics
        foreach (var (name, durations) in _operationDurations)
        {
            if (!durations.Any())
                continue;

            stats.Operations[name] = new OperationStatistics
            {
                Name = name,
                Count = durations.Count,
                TotalDuration = durations.Sum(),
                AverageDuration = durations.Average(),
                MinDuration = durations.Min(),
                MaxDuration = durations.Max(),
                StandardDeviation = CalculateStandardDeviation(durations)
            };
        }

        // Calculate metric statistics
        foreach (var (name, values) in _metricValues)
        {
            if (!values.Any())
                continue;

            stats.Metrics[name] = new MetricStatistics
            {
                Name = name,
                Count = values.Count,
                Total = values.Sum(),
                Average = values.Average(),
                Min = values.Min(),
                Max = values.Max()
            };
        }

        // Copy event counts
        stats.EventCounts = new Dictionary<string, int>(_eventCounts);

        return stats;
    }

    public void Reset()
    {
        _operationDurations.Clear();
        _metricValues.Clear();
        _eventCounts.Clear();
        _logger.LogInformation("Performance metrics reset");
    }

    public async Task ExportMetricsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var stats = GetStatistics();
        
        var report = new
        {
            ExportedAt = DateTime.UtcNow,
            Statistics = stats,
            SystemInfo = new
            {
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                OSVersion = Environment.OSVersion.ToString(),
                Runtime = Environment.Version.ToString(),
                WorkingSet = _currentProcess.WorkingSet64,
                PrivateMemory = _currentProcess.PrivateMemorySize64
            }
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("Performance metrics exported to {FilePath}", filePath);
    }

    private void RecordOperationDuration(string operationName, double duration, Dictionary<string, object>? tags)
    {
        _operationDurations.AddOrUpdate(operationName,
            new List<double> { duration },
            (key, list) =>
            {
                list.Add(duration);
                return list;
            });

        _logger.LogDebug("Operation {OperationName} completed in {Duration:F2}ms", operationName, duration);
    }

    private double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count <= 1)
            return 0;

        var average = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }

    private double GetCpuUsage()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = _currentProcess.TotalProcessorTime;
            
            Thread.Sleep(100); // Small delay for measurement
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = _currentProcess.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return Math.Round(cpuUsageTotal * 100, 2);
        }
        catch
        {
            return 0;
        }
    }

    private class OperationMeasurement : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly Dictionary<string, object>? _tags;
        private readonly Stopwatch _stopwatch;

        public OperationMeasurement(PerformanceMonitor monitor, string operationName, Dictionary<string, object>? tags)
        {
            _monitor = monitor;
            _operationName = operationName;
            _tags = tags;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.RecordOperationDuration(_operationName, _stopwatch.Elapsed.TotalMilliseconds, _tags);
        }
    }
}