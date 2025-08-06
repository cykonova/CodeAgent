using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CodeAgent.Core.Services;

public interface IRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3);
    Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, int maxRetries = 3);
}

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;

    public RetryService(ILogger<RetryService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3)
    {
        var retryPolicy = Policy
            .Handle<Exception>(ex => IsRetryableException(ex))
            .WaitAndRetryAsync(
                maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} of {MaxRetries} for operation {OperationName} after {Delay}ms. Error: {Error}",
                        retryCount, maxRetries, operationName, timeSpan.TotalMilliseconds, exception.Message);
                });

        try
        {
            return await retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogDebug("Executing operation {OperationName}", operationName);
                return await operation();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation {OperationName} failed after {MaxRetries} retries", operationName, maxRetries);
            throw;
        }
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, int maxRetries = 3)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, operationName, maxRetries);
    }

    private bool IsRetryableException(Exception exception)
    {
        // Network-related exceptions
        if (exception is HttpRequestException ||
            exception is TaskCanceledException ||
            exception is TimeoutException)
        {
            return true;
        }

        // Check for specific error messages
        var message = exception.Message?.ToLower() ?? string.Empty;
        if (message.Contains("timeout") ||
            message.Contains("network") ||
            message.Contains("connection") ||
            message.Contains("rate limit") ||
            message.Contains("temporarily"))
        {
            return true;
        }

        // Check inner exception
        if (exception.InnerException != null)
        {
            return IsRetryableException(exception.InnerException);
        }

        return false;
    }
}