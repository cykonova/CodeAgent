using CodeAgent.Domain.Interfaces;
using CodeAgent.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Web.Tests;

public class AnalyticsServiceTests
{
    private readonly AnalyticsService _analyticsService;
    private readonly Mock<ILogger<AnalyticsService>> _loggerMock;

    public AnalyticsServiceTests()
    {
        _loggerMock = new Mock<ILogger<AnalyticsService>>();
        _analyticsService = new AnalyticsService(_loggerMock.Object);
    }

    [Fact]
    public async Task TrackEventAsync_RecordsEvent()
    {
        // Arrange
        var eventName = "TestEvent";
        var properties = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        await _analyticsService.TrackEventAsync(eventName, properties);
        var report = await _analyticsService.GenerateReportAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        report.Should().NotBeNull();
        report.TotalEvents.Should().BeGreaterThan(0);
        report.EventCounts.Should().ContainKey(eventName);
    }

    [Fact]
    public async Task TrackMetricAsync_RecordsMetric()
    {
        // Arrange
        var metricName = "test.metric";
        var value = 42.5;

        // Act
        await _analyticsService.TrackMetricAsync(metricName, value);
        var report = await _analyticsService.GenerateReportAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        report.Metrics.Should().ContainKey($"{metricName}_avg");
        report.Metrics[$"{metricName}_avg"].Should().Be(value);
    }

    [Fact]
    public async Task TrackExceptionAsync_RecordsException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        await _analyticsService.TrackExceptionAsync(exception);
        var report = await _analyticsService.GenerateReportAsync(
            DateTime.UtcNow.AddMinutes(-1), 
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        report.EventCounts.Should().ContainKey("Exception");
        report.EventCounts["Exception"].Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTopFeaturesAsync_ReturnsTopFeatures()
    {
        // Arrange
        await _analyticsService.TrackEventAsync("Feature1");
        await _analyticsService.TrackEventAsync("Feature1");
        await _analyticsService.TrackEventAsync("Feature2");

        // Act
        var topFeatures = await _analyticsService.GetTopFeaturesAsync(10);

        // Assert
        topFeatures.Should().NotBeEmpty();
        var feature1 = topFeatures.FirstOrDefault(f => f.Name == "Feature1");
        feature1.Should().NotBeNull();
        feature1!.UsageCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUsageStatisticsAsync_ReturnsUserStats()
    {
        // Arrange
        var userId = "test-user";
        await _analyticsService.TrackEventAsync("UserAction");

        // Act
        var stats = await _analyticsService.GetUsageStatisticsAsync(userId);

        // Assert
        stats.Should().NotBeNull();
        stats.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GenerateReportAsync_GeneratesCompleteReport()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        
        await _analyticsService.TrackEventAsync("Event1");
        await _analyticsService.TrackEventAsync("Event2");
        await _analyticsService.TrackMetricAsync("metric1", 10);
        await _analyticsService.TrackMetricAsync("metric1", 20);

        // Act
        var report = await _analyticsService.GenerateReportAsync(from, to);

        // Assert
        report.Should().NotBeNull();
        report.PeriodStart.Should().Be(from);
        report.PeriodEnd.Should().Be(to);
        report.TotalEvents.Should().Be(2);
        report.EventCounts.Should().HaveCount(2);
        report.Metrics.Should().ContainKey("metric1_avg");
        report.Metrics["metric1_avg"].Should().Be(15); // Average of 10 and 20
    }
}