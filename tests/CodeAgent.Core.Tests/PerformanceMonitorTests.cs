using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests;

public class PerformanceMonitorTests
{
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly Mock<ILogger<PerformanceMonitor>> _loggerMock;

    public PerformanceMonitorTests()
    {
        _loggerMock = new Mock<ILogger<PerformanceMonitor>>();
        _performanceMonitor = new PerformanceMonitor(_loggerMock.Object);
    }

    [Fact]
    public void StartMeasurement_RecordsOperationDuration()
    {
        // Act
        using (_performanceMonitor.StartMeasurement("test-operation"))
        {
            Thread.Sleep(50); // Simulate work
        }

        var stats = _performanceMonitor.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.Operations.Should().ContainKey("test-operation");
        stats.Operations["test-operation"].Count.Should().Be(1);
        stats.Operations["test-operation"].TotalDuration.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordMetric_StoresMetricValue()
    {
        // Act
        _performanceMonitor.RecordMetric("test-metric", 42.5);
        _performanceMonitor.RecordMetric("test-metric", 37.5);
        
        var stats = _performanceMonitor.GetStatistics();

        // Assert
        stats.Metrics.Should().ContainKey("test-metric");
        stats.Metrics["test-metric"].Count.Should().Be(2);
        stats.Metrics["test-metric"].Average.Should().Be(40.0);
        stats.Metrics["test-metric"].Min.Should().Be(37.5);
        stats.Metrics["test-metric"].Max.Should().Be(42.5);
    }

    [Fact]
    public void RecordEvent_IncrementsEventCount()
    {
        // Act
        _performanceMonitor.RecordEvent("test-event");
        _performanceMonitor.RecordEvent("test-event");
        _performanceMonitor.RecordEvent("another-event");
        
        var stats = _performanceMonitor.GetStatistics();

        // Assert
        stats.EventCounts.Should().ContainKey("test-event");
        stats.EventCounts["test-event"].Should().Be(2);
        stats.EventCounts["another-event"].Should().Be(1);
    }

    [Fact]
    public void GetStatistics_ReturnsCompleteStatistics()
    {
        // Arrange
        _performanceMonitor.RecordMetric("metric1", 100);
        _performanceMonitor.RecordEvent("event1");
        
        using (_performanceMonitor.StartMeasurement("operation1"))
        {
            Thread.Sleep(10);
        }

        // Act
        var stats = _performanceMonitor.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        stats.TotalRuntime.Should().BeGreaterThan(TimeSpan.Zero);
        stats.TotalMemoryUsed.Should().BeGreaterThan(0);
        stats.Operations.Should().HaveCount(1);
        stats.Metrics.Should().HaveCount(1);
        stats.EventCounts.Should().HaveCount(1);
    }

    [Fact]
    public void Reset_ClearsAllMetrics()
    {
        // Arrange
        _performanceMonitor.RecordMetric("metric1", 100);
        _performanceMonitor.RecordEvent("event1");
        
        // Act
        _performanceMonitor.Reset();
        var stats = _performanceMonitor.GetStatistics();

        // Assert
        stats.Operations.Should().BeEmpty();
        stats.Metrics.Should().BeEmpty();
        stats.EventCounts.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportMetricsAsync_CreatesJsonFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        _performanceMonitor.RecordMetric("test-metric", 123);
        _performanceMonitor.RecordEvent("test-event");

        try
        {
            // Act
            await _performanceMonitor.ExportMetricsAsync(tempFile);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("test-metric");
            content.Should().Contain("test-event");
            content.Should().Contain("Statistics");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void MultipleOperations_CalculatesCorrectStatistics()
    {
        // Act
        for (int i = 0; i < 5; i++)
        {
            using (_performanceMonitor.StartMeasurement("repeated-op"))
            {
                Thread.Sleep(10);
            }
        }

        var stats = _performanceMonitor.GetStatistics();

        // Assert
        stats.Operations["repeated-op"].Count.Should().Be(5);
        stats.Operations["repeated-op"].AverageDuration.Should().BeGreaterThan(0);
        stats.Operations["repeated-op"].StandardDeviation.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void RecordMetric_WithTags_LogsDebugInformation()
    {
        // Arrange
        var tags = new Dictionary<string, object> { { "source", "test" } };

        // Act
        _performanceMonitor.RecordMetric("tagged-metric", 100, tags);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("tagged-metric")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordEvent_WithProperties_LogsDebugInformation()
    {
        // Arrange
        var properties = new Dictionary<string, object> { { "user", "test-user" } };

        // Act
        _performanceMonitor.RecordEvent("user-action", properties);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("user-action")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}