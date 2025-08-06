using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using CodeAgent.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Web.Tests;

public class CleanupServiceTests
{
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<CleanupService>> _loggerMock;
    private readonly CleanupService _cleanupService;

    public CleanupServiceTests()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<CleanupService>>();
        _cleanupService = new CleanupService(
            _sessionServiceMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CleansExpiredSessions()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session 
            { 
                Id = "old-session", 
                LastModified = DateTime.UtcNow.AddDays(-40) // Older than 30 days
            },
            new Session 
            { 
                Id = "recent-session", 
                LastModified = DateTime.UtcNow.AddDays(-10) // Within 30 days
            }
        };

        _sessionServiceMock.Setup(x => x.GetAllSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var cts = new CancellationTokenSource();
        
        // Act
        var executeTask = _cleanupService.StartAsync(cts.Token);
        await Task.Delay(100); // Give it time to start
        await cts.CancelAsync();
        await executeTask;

        // Assert
        _sessionServiceMock.Verify(x => x.DeleteSessionAsync("old-session", It.IsAny<CancellationToken>()), Times.Once);
        _sessionServiceMock.Verify(x => x.DeleteSessionAsync("recent-session", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ClearsExpiredCache()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = _cleanupService.StartAsync(cts.Token);
        await Task.Delay(100); // Give it time to start
        await cts.CancelAsync();
        await executeTask;

        // Assert
        _cacheServiceMock.Verify(x => x.ClearExpiredAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesExceptions()
    {
        // Arrange
        _sessionServiceMock.Setup(x => x.GetAllSessionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var cts = new CancellationTokenSource();

        // Act
        var executeTask = _cleanupService.StartAsync(cts.Token);
        await Task.Delay(100); // Give it time to start and handle exception
        await cts.CancelAsync();
        await executeTask;

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error during cleanup")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StopAsync_StopsGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        await _cleanupService.StartAsync(cts.Token);
        await Task.Delay(50);
        await _cleanupService.StopAsync(cts.Token);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleanup service stopped")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}