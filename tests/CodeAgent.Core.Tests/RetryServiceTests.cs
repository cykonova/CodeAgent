using CodeAgent.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests;

public class RetryServiceTests
{
    private readonly Mock<ILogger<RetryService>> _loggerMock;
    private readonly RetryService _retryService;

    public RetryServiceTests()
    {
        _loggerMock = new Mock<ILogger<RetryService>>();
        _retryService = new RetryService(_loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SucceedsOnFirstAttempt()
    {
        // Arrange
        var expectedResult = "success";
        var attempts = 0;

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(
            async () =>
            {
                attempts++;
                await Task.Delay(1);
                return expectedResult;
            },
            "TestOperation");

        // Assert
        result.Should().Be(expectedResult);
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesOnRetryableException()
    {
        // Arrange
        var expectedResult = "success";
        var attempts = 0;

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(
            async () =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new HttpRequestException("Network error");
                }
                await Task.Delay(1);
                return expectedResult;
            },
            "TestOperation");

        // Assert
        result.Should().Be(expectedResult);
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ThrowsAfterMaxRetries()
    {
        // Arrange
        var attempts = 0;

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    attempts++;
                    await Task.Delay(1);
                    throw new HttpRequestException("Persistent network error");
                },
                "TestOperation",
                maxRetries: 2);
        });

        attempts.Should().Be(3); // Initial attempt + 2 retries
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_DoesNotRetryNonRetryableException()
    {
        // Arrange
        var attempts = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    attempts++;
                    await Task.Delay(1);
                    throw new InvalidOperationException("Non-retryable error");
                },
                "TestOperation",
                maxRetries: 3);
        });

        attempts.Should().Be(1); // Only the initial attempt
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesOnTimeout()
    {
        // Arrange
        var expectedResult = "success";
        var attempts = 0;

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(
            async () =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new TimeoutException("Request timed out");
                }
                await Task.Delay(1);
                return expectedResult;
            },
            "TestOperation");

        // Assert
        result.Should().Be(expectedResult);
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesOnTaskCanceledException()
    {
        // Arrange
        var expectedResult = "success";
        var attempts = 0;

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(
            async () =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new TaskCanceledException("Operation cancelled");
                }
                await Task.Delay(1);
                return expectedResult;
            },
            "TestOperation");

        // Assert
        result.Should().Be(expectedResult);
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_SucceedsAfterRetry()
    {
        // Arrange
        var attempts = 0;

        // Act
        await _retryService.ExecuteWithRetryAsync(
            async () =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new HttpRequestException("Temporary error");
                }
                await Task.Delay(1);
            },
            "TestOperation");

        // Assert
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_LogsRetryAttempts()
    {
        // Arrange
        var attempts = 0;

        // Act
        await _retryService.ExecuteWithRetryAsync(
            async () =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new HttpRequestException("Network error");
                }
                await Task.Delay(1);
                return "success";
            },
            "TestOperation");

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_LogsFailureAfterMaxRetries()
    {
        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync(
                async () =>
                {
                    await Task.Delay(1);
                    throw new HttpRequestException("Persistent error");
                },
                "TestOperation",
                maxRetries: 1);
        });

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed after")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}