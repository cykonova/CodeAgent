# Phase 2: Reliability & Resilience
**Duration**: Week 3-4  
**Priority**: HIGH  
**Dependencies**: Phase 1 (Security & Data)

## Overview
Implement fault tolerance, retry mechanisms, and resilience patterns to ensure the API can handle failures gracefully and maintain availability under adverse conditions.

## Tasks

### 2.1 Implement Polly Resilience Patterns (3 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Add retry policies for transient failures
- Implement circuit breaker pattern
- Add timeout policies
- Configure bulkhead isolation
- Create fallback strategies

#### Implementation Example
```csharp
// In Program.cs or ServiceConfiguration
builder.Services.AddHttpClient<ILLMProvider, AnthropicProvider>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var logger = context.Values["logger"] as ILogger;
                logger?.LogWarning("Retry {RetryCount} after {TimeSpan}ms", 
                    retryCount, timespan.TotalMilliseconds);
            });
}

private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            5,
            TimeSpan.FromSeconds(30),
            onBreak: (result, timespan) => 
            {
                // Log circuit breaker open
            },
            onReset: () => 
            {
                // Log circuit breaker reset
            });
}
```

#### Policies to Implement
| Service | Retry | Circuit Breaker | Timeout | Bulkhead |
|---------|-------|-----------------|---------|----------|
| LLM Providers | 3x exponential | 5 failures/30s | 30s | 10 concurrent |
| Database | 2x linear | N/A | 5s | N/A |
| External APIs | 3x exponential | 3 failures/60s | 10s | 5 concurrent |
| Message Queue | 5x exponential | 10 failures/60s | 5s | 20 concurrent |

#### Acceptance Criteria
- [ ] All external calls use Polly policies
- [ ] Circuit breakers prevent cascade failures
- [ ] Retry logic with exponential backoff
- [ ] Timeout policies prevent hanging requests
- [ ] Bulkhead isolation for resource protection

### 2.2 Global Exception Handling (2 days)
**Owner**: Backend Team  
**Complexity**: Low

#### Requirements
- Create global exception middleware
- Standardize error response format
- Log all exceptions with context
- Hide sensitive information in production
- Add correlation IDs to errors

#### Error Response Format
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "The request contains invalid data",
    "timestamp": "2024-01-01T12:00:00Z",
    "correlationId": "abc-123-def",
    "details": [
      {
        "field": "email",
        "message": "Invalid email format"
      }
    ]
  }
}
```

#### Exception Middleware Implementation
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        
        _logger.LogError(ex, "Unhandled exception occurred. CorrelationId: {CorrelationId}", 
            correlationId);

        var response = new ErrorResponse
        {
            Code = GetErrorCode(exception),
            Message = GetErrorMessage(exception),
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow
        };

        if (_environment.IsDevelopment())
        {
            response.StackTrace = exception.StackTrace;
        }

        context.Response.StatusCode = GetStatusCode(exception);
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

#### Exception Types to Handle
- ValidationException → 400 Bad Request
- UnauthorizedException → 401 Unauthorized
- ForbiddenException → 403 Forbidden
- NotFoundException → 404 Not Found
- ConflictException → 409 Conflict
- TimeoutException → 408 Request Timeout
- ExternalServiceException → 503 Service Unavailable
- Exception → 500 Internal Server Error

#### Acceptance Criteria
- [ ] All exceptions handled globally
- [ ] Consistent error format
- [ ] Sensitive data never exposed
- [ ] All errors logged with context
- [ ] Correlation IDs in all responses

### 2.3 Health Checks Implementation (2 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Add health check endpoints
- Check all critical dependencies
- Implement readiness vs liveness probes
- Add degraded state support
- Create health check UI

#### Health Checks to Implement
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddRedis(redisConnectionString, name: "redis-cache")
    .AddUrlGroup(new Uri("https://api.anthropic.com/health"), "anthropic-api")
    .AddUrlGroup(new Uri("https://api.openai.com/health"), "openai-api")
    .AddCheck<CustomHealthCheck>("custom-check")
    .AddCheck<DiskSpaceHealthCheck>("disk-space")
    .AddCheck<MemoryHealthCheck>("memory");

// Configure health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
```

#### Custom Health Check Example
```csharp
public class MessageQueueHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check message queue connectivity
            var isHealthy = await CheckMessageQueueAsync();
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Message queue is accessible");
            }
            
            return HealthCheckResult.Degraded("Message queue is slow");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Message queue is unavailable", ex);
        }
    }
}
```

#### Acceptance Criteria
- [ ] /health endpoint returns overall status
- [ ] /health/ready for Kubernetes readiness
- [ ] /health/live for Kubernetes liveness
- [ ] All dependencies checked
- [ ] Health check UI available
- [ ] Degraded state properly reported

### 2.4 Distributed Caching with Redis (3 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Set up Redis connection
- Implement cache-aside pattern
- Add cache invalidation strategy
- Configure cache expiration policies
- Add cache health monitoring

#### Cache Implementation
```csharp
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;

    public async Task<T> GetOrSetAsync<T>(
        string key, 
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        try
        {
            var cached = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(cached);
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            var value = await factory();
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };
            
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
            return value;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis cache failed, falling back to factory");
            return await factory();
        }
    }
}
```

#### Cache Strategy
| Data Type | TTL | Invalidation Strategy |
|-----------|-----|----------------------|
| User Sessions | 30 min | Sliding expiration |
| Provider Config | 1 hour | Manual invalidation |
| Project Metadata | 15 min | Event-based |
| Agent Definitions | 5 min | Time-based |
| API Responses | 1 min | Time-based |

#### Acceptance Criteria
- [ ] Redis configured and connected
- [ ] Cache service implemented
- [ ] Cache-aside pattern working
- [ ] Invalidation strategies defined
- [ ] Cache metrics available
- [ ] Fallback when cache unavailable

### 2.5 Connection Resilience (2 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Configure database connection resilience
- Implement connection pooling
- Add automatic reconnection logic
- Configure proper timeouts
- Monitor connection health

#### Database Resilience
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        
        sqlOptions.CommandTimeout(30);
    });
});
```

#### WebSocket Resilience
```csharp
public class ResilientWebSocketHandler
{
    private async Task MaintainConnection(WebSocket socket)
    {
        while (true)
        {
            try
            {
                await ProcessMessages(socket);
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "WebSocket disconnected, attempting reconnection");
                await Task.Delay(TimeSpan.FromSeconds(5));
                socket = await ReconnectAsync();
            }
        }
    }
}
```

#### Acceptance Criteria
- [ ] Database resilience configured
- [ ] Connection pooling optimized
- [ ] Auto-reconnection implemented
- [ ] Proper timeout values set
- [ ] Connection metrics tracked

## Testing Requirements

### Chaos Engineering Tests
- Simulate network failures
- Database connection drops
- Redis unavailability
- Provider API failures
- High latency scenarios

### Load Testing
- Concurrent user limits
- Request throughput
- Cache performance
- Circuit breaker triggers
- Resource exhaustion

## Monitoring & Alerts

### Key Metrics
- Circuit breaker state changes
- Retry attempt counts
- Cache hit/miss ratios
- Health check failures
- Exception rates by type

### Alert Thresholds
| Metric | Warning | Critical |
|--------|---------|----------|
| Error Rate | > 1% | > 5% |
| Circuit Breaker Opens | > 2/hour | > 5/hour |
| Cache Hit Rate | < 80% | < 60% |
| Health Check Failures | 1 | 3 consecutive |
| Response Time p95 | > 1s | > 3s |

## Performance Targets
- API response time p50: < 200ms
- API response time p95: < 1s
- API response time p99: < 3s
- Cache hit rate: > 85%
- Error rate: < 0.1%
- Availability: > 99.9%

## Rollback Strategy
1. Feature flags for new resilience patterns
2. Gradual rollout with monitoring
3. Quick disable switches for each pattern
4. Fallback to previous behavior

## Dependencies
- Redis server provisioned
- Monitoring infrastructure ready
- Load testing tools available
- Chaos engineering framework

## Deliverables
- [ ] Polly policies configuration
- [ ] Global exception handler
- [ ] Health check implementation
- [ ] Redis cache service
- [ ] Connection resilience code
- [ ] Chaos test results
- [ ] Performance baseline report
- [ ] Runbook for failure scenarios

## Notes
- Consider implementing saga pattern for distributed transactions
- Evaluate need for message queue (RabbitMQ/Azure Service Bus)
- Document SLA targets based on resilience patterns
- Plan for geographic redundancy in future phases