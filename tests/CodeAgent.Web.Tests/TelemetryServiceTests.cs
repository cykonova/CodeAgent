using CodeAgent.Domain.Interfaces;
using CodeAgent.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;

namespace CodeAgent.Web.Tests;

public class TelemetryServiceTests
{
    private readonly TelemetryService _telemetryService;
    private readonly Mock<ILogger<TelemetryService>> _loggerMock;

    public TelemetryServiceTests()
    {
        _loggerMock = new Mock<ILogger<TelemetryService>>();
        _telemetryService = new TelemetryService(_loggerMock.Object);
    }

    [Fact]
    public void StartActivity_CreatesNewActivity()
    {
        // Arrange
        var activityName = "TestActivity";

        // Act
        var activity = _telemetryService.StartActivity(activityName);

        // Assert
        activity.Should().NotBeNull();
        activity.DisplayName.Should().Be(activityName);
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public void StartActivity_WithTags_IncludesTags()
    {
        // Arrange
        var activityName = "TestActivity";
        var tags = new Dictionary<string, object?>
        {
            ["user.id"] = "user-123",
            ["session.id"] = "session-456"
        };

        // Act
        var activity = _telemetryService.StartActivity(activityName, tags);

        // Assert
        activity.Should().NotBeNull();
        activity.Tags.Should().Contain(t => t.Key == "user.id" && t.Value == "user-123");
        activity.Tags.Should().Contain(t => t.Key == "session.id" && t.Value == "session-456");
    }

    [Fact]
    public async Task RecordMetricAsync_RecordsCounter()
    {
        // Arrange
        var metricName = "test.counter";
        var value = 5;
        var tags = new Dictionary<string, object?> { ["env"] = "test" };

        // Act
        await _telemetryService.RecordMetricAsync(metricName, value, tags);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Recorded metric {metricName}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task RecordExceptionAsync_RecordsException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = new Dictionary<string, object?>
        {
            ["operation"] = "TestOperation",
            ["userId"] = "user-123"
        };

        // Act
        await _telemetryService.RecordExceptionAsync(exception, context);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception recorded")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ReturnsHealthyStatus()
    {
        // Act
        var health = await _telemetryService.GetHealthStatusAsync();

        // Assert
        health.Should().NotBeNull();
        health.Status.Should().Be("Healthy");
        health.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
        health.Checks.Should().NotBeNull();
        health.Checks.Should().ContainKey("telemetry");
        health.Checks["telemetry"].Should().Be("OK");
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsCurrentMetrics()
    {
        // Arrange
        await _telemetryService.RecordMetricAsync("test.metric1", 10);
        await _telemetryService.RecordMetricAsync("test.metric2", 20);

        // Act
        var metrics = await _telemetryService.GetMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.Should().ContainKey("process.cpu.usage");
        metrics.Should().ContainKey("process.memory.usage");
        metrics.Should().ContainKey("gc.heap.size");
    }

    [Fact]
    public async Task GetTracesAsync_ReturnsRecentTraces()
    {
        // Arrange
        using (var activity1 = _telemetryService.StartActivity("Activity1"))
        {
            activity1?.SetStatus(ActivityStatusCode.Ok);
        }
        using (var activity2 = _telemetryService.StartActivity("Activity2"))
        {
            activity2?.SetStatus(ActivityStatusCode.Error, "Test error");
        }

        // Act
        var traces = await _telemetryService.GetTracesAsync(10);

        // Assert
        traces.Should().NotBeNull();
        traces.Should().NotBeEmpty();
        traces.Should().HaveCountLessOrEqualTo(10);
    }

    [Fact]
    public void StartActivity_WithParentContext_CreatesChildActivity()
    {
        // Arrange
        var parentActivity = _telemetryService.StartActivity("ParentActivity");
        var parentContext = parentActivity?.Context ?? default;

        // Act
        var childActivity = _telemetryService.StartActivity("ChildActivity", parentContext: parentContext);

        // Assert
        childActivity.Should().NotBeNull();
        if (parentActivity != null)
        {
            childActivity!.ParentId.Should().Be(parentActivity.Id);
        }
    }

    [Fact]
    public async Task RecordMetricAsync_HandlesNullTags()
    {
        // Arrange
        var metricName = "test.metric";
        var value = 42;

        // Act
        var act = async () => await _telemetryService.RecordMetricAsync(metricName, value, null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetDiagnosticsAsync_ReturnsDetailedInfo()
    {
        // Act
        var diagnostics = await _telemetryService.GetDiagnosticsAsync();

        // Assert
        diagnostics.Should().NotBeNull();
        diagnostics.Should().ContainKey("runtime");
        diagnostics.Should().ContainKey("environment");
        diagnostics.Should().ContainKey("process");
        
        var runtime = diagnostics["runtime"] as Dictionary<string, object>;
        runtime.Should().NotBeNull();
        runtime!.Should().ContainKey("version");
        runtime.Should().ContainKey("gcMode");
    }

    [Fact]
    public void SetGlobalTag_AddsTagToAllActivities()
    {
        // Arrange
        _telemetryService.SetGlobalTag("environment", "test");
        _telemetryService.SetGlobalTag("version", "1.0.0");

        // Act
        var activity = _telemetryService.StartActivity("TestActivity");

        // Assert
        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == "environment" && t.Value == "test");
        activity.Tags.Should().Contain(t => t.Key == "version" && t.Value == "1.0.0");
    }

    [Fact]
    public async Task ExportMetricsAsync_ReturnsOpenTelemetryFormat()
    {
        // Arrange
        await _telemetryService.RecordMetricAsync("test.metric", 100);

        // Act
        var exported = await _telemetryService.ExportMetricsAsync("otlp");

        // Assert
        exported.Should().NotBeNull();
        exported.Should().ContainKey("resourceMetrics");
        var resourceMetrics = exported["resourceMetrics"] as List<object>;
        resourceMetrics.Should().NotBeNull();
    }
}