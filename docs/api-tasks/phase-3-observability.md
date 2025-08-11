# Phase 3: Observability & Monitoring
**Duration**: Week 5  
**Priority**: HIGH  
**Dependencies**: Phase 2 (Reliability)

## Overview
Implement comprehensive observability to understand system behavior, diagnose issues quickly, and maintain SLAs. This phase establishes the foundation for data-driven operations and proactive issue resolution.

## Tasks

### 3.1 Application Insights / OpenTelemetry (2 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Configure OpenTelemetry with multiple exporters
- Set up Application Insights or alternative
- Implement custom metrics collection
- Configure sampling strategies
- Add performance counters

#### OpenTelemetry Configuration
```csharp
// In Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("CodeAgent.API")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("CodeAgent.Gateway", serviceVersion: "1.0.0"))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = (httpContext) =>
                {
                    // Don't trace health checks
                    return !httpContext.Request.Path.StartsWithSegments("/health");
                };
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.RecordException = true;
            })
            .AddRedisInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://otel-collector:4317");
            })
            .AddConsoleExporter(); // Development only
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .AddMeter("CodeAgent.Metrics")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter()
            .AddOtlpExporter();
    });
```

#### Custom Metrics Implementation
```csharp
public class MetricsService
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly ObservableGauge<int> _activeConnections;

    public MetricsService()
    {
        _meter = new Meter("CodeAgent.Metrics", "1.0.0");
        
        _requestCounter = _meter.CreateCounter<long>(
            "codeagent.requests.total",
            "requests",
            "Total number of requests");
            
        _requestDuration = _meter.CreateHistogram<double>(
            "codeagent.request.duration",
            "milliseconds",
            "Request duration in milliseconds");
            
        _activeConnections = _meter.CreateObservableGauge<int>(
            "codeagent.connections.active",
            () => GetActiveConnectionCount(),
            "connections",
            "Number of active WebSocket connections");
    }

    public void RecordRequest(string endpoint, string method, int statusCode)
    {
        _requestCounter.Add(1, 
            new KeyValuePair<string, object>("endpoint", endpoint),
            new KeyValuePair<string, object>("method", method),
            new KeyValuePair<string, object>("status", statusCode));
    }
}
```

#### Metrics to Track
| Category | Metric | Type | Description |
|----------|--------|------|-------------|
| API | request_count | Counter | Total requests by endpoint |
| API | request_duration | Histogram | Response time distribution |
| API | error_rate | Gauge | Percentage of failed requests |
| WebSocket | active_connections | Gauge | Current WebSocket connections |
| WebSocket | message_throughput | Counter | Messages per second |
| Providers | llm_requests | Counter | Requests per provider |
| Providers | llm_latency | Histogram | Provider response times |
| Providers | token_usage | Counter | Tokens consumed |
| Cache | hit_rate | Gauge | Cache hit percentage |
| Cache | eviction_count | Counter | Cache evictions |
| Database | query_duration | Histogram | Query execution time |
| Database | connection_pool | Gauge | Active connections |

#### Acceptance Criteria
- [ ] OpenTelemetry configured
- [ ] Traces for all requests
- [ ] Custom metrics implemented
- [ ] Sampling configured
- [ ] Dashboards created
- [ ] Alerts configured

### 3.2 Structured Logging with Serilog (1 day)
**Owner**: Backend Team  
**Complexity**: Low

#### Requirements
- Replace default logging with Serilog
- Configure structured logging
- Add contextual enrichers
- Set up multiple sinks
- Implement log correlation

#### Serilog Configuration
```csharp
// In Program.cs
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithCorrelationId()
        .Enrich.WithProperty("Application", "CodeAgent.Gateway")
        .WriteTo.Console(new CompactJsonFormatter())
        .WriteTo.ApplicationInsights(services.GetRequiredService<TelemetryConfiguration>(), 
            TelemetryConverter.Traces)
        .WriteTo.File(
            new CompactJsonFormatter(),
            "logs/codeagent-.json",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30)
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
        {
            AutoRegisterTemplate = true,
            IndexFormat = "codeagent-{0:yyyy.MM.dd}",
            BatchPostingLimit = 50,
            Period = TimeSpan.FromSeconds(2)
        });
});
```

#### Logging Standards
```csharp
public class ProviderService
{
    private readonly ILogger<ProviderService> _logger;

    public async Task<Response> CallProviderAsync(Request request)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["ProviderId"] = request.ProviderId,
            ["RequestId"] = request.Id,
            ["UserId"] = request.UserId
        }))
        {
            _logger.LogInformation("Starting provider call for {Provider}", request.ProviderId);
            
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await ExecuteCallAsync(request);
                
                _logger.LogInformation(
                    "Provider call completed successfully in {ElapsedMs}ms with {TokenCount} tokens",
                    stopwatch.ElapsedMilliseconds,
                    response.TokenCount);
                    
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Provider call failed for {Provider} after {ElapsedMs}ms",
                    request.ProviderId,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
```

#### Log Levels by Environment
| Level | Development | Staging | Production |
|-------|------------|---------|------------|
| Verbose | Yes | No | No |
| Debug | Yes | Yes | No |
| Information | Yes | Yes | Yes |
| Warning | Yes | Yes | Yes |
| Error | Yes | Yes | Yes |
| Fatal | Yes | Yes | Yes |

#### Acceptance Criteria
- [ ] Serilog integrated
- [ ] Structured logging everywhere
- [ ] Log correlation working
- [ ] Multiple sinks configured
- [ ] Log retention policies set
- [ ] Sensitive data masked

### 3.3 Distributed Tracing (1 day)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Implement correlation IDs
- Trace across service boundaries
- Add custom trace spans
- Configure trace sampling
- Visualize trace data

#### Correlation ID Middleware
```csharp
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        context.TraceIdentifier = correlationId;
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
    
    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            return correlationId;
        }
        
        return Guid.NewGuid().ToString();
    }
}
```

#### Custom Trace Spans
```csharp
public class AgentOrchestrator
{
    private readonly ITracer _tracer;

    public async Task<AgentResponse> ExecuteWorkflowAsync(WorkflowRequest request)
    {
        using var activity = Activity.StartActivity("ExecuteWorkflow");
        activity?.SetTag("workflow.id", request.WorkflowId);
        activity?.SetTag("workflow.name", request.WorkflowName);
        
        try
        {
            using (var span = _tracer.StartActiveSpan("ValidateWorkflow"))
            {
                await ValidateWorkflowAsync(request);
            }
            
            using (var span = _tracer.StartActiveSpan("ExecuteSteps"))
            {
                foreach (var step in request.Steps)
                {
                    using (var stepSpan = _tracer.StartActiveSpan($"ExecuteStep:{step.Name}"))
                    {
                        stepSpan.SetAttribute("step.type", step.Type);
                        await ExecuteStepAsync(step);
                    }
                }
            }
            
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

#### Acceptance Criteria
- [ ] Correlation IDs in all requests
- [ ] Traces span service boundaries
- [ ] Custom spans for key operations
- [ ] Trace visualization available
- [ ] Sampling configured appropriately

### 3.4 Monitoring Dashboards (1 day)
**Owner**: DevOps Team  
**Complexity**: Low

#### Requirements
- Create Grafana dashboards
- Set up Prometheus queries
- Configure auto-refresh
- Add drill-down capabilities
- Create mobile-friendly views

#### Dashboard Layouts

**1. System Overview Dashboard**
- Request rate (requests/sec)
- Error rate (percentage)
- Response time (p50, p95, p99)
- Active users
- System resources (CPU, Memory, Disk)

**2. API Performance Dashboard**
- Endpoint latency heatmap
- Top 10 slowest endpoints
- Request distribution by endpoint
- Status code distribution
- Request payload sizes

**3. Provider Monitoring Dashboard**
- Provider availability
- Response times by provider
- Token usage and costs
- Error rates by provider
- Rate limit tracking

**4. WebSocket Dashboard**
- Active connections
- Message throughput
- Connection duration distribution
- Disconnection reasons
- Message size distribution

**5. Infrastructure Dashboard**
- Container metrics
- Database performance
- Redis cache metrics
- Network I/O
- Disk usage

#### Grafana Dashboard JSON Example
```json
{
  "dashboard": {
    "title": "CodeAgent API Overview",
    "panels": [
      {
        "title": "Request Rate",
        "targets": [
          {
            "expr": "sum(rate(codeagent_requests_total[5m]))",
            "legendFormat": "Requests/sec"
          }
        ],
        "type": "graph"
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "sum(rate(codeagent_requests_total{status=~\"5..\"}[5m])) / sum(rate(codeagent_requests_total[5m])) * 100",
            "legendFormat": "Error %"
          }
        ],
        "type": "gauge",
        "thresholds": [
          { "value": 1, "color": "yellow" },
          { "value": 5, "color": "red" }
        ]
      }
    ]
  }
}
```

#### Acceptance Criteria
- [ ] All dashboards created
- [ ] Real-time data updates
- [ ] Drill-down capabilities
- [ ] Mobile responsive
- [ ] Alerts integrated

### 3.5 Alerting Rules (1 day)
**Owner**: DevOps Team  
**Complexity**: Low

#### Requirements
- Define alert rules
- Configure notification channels
- Set up escalation policies
- Create runbooks
- Test alert scenarios

#### Alert Definitions
```yaml
groups:
  - name: api_alerts
    rules:
      - alert: HighErrorRate
        expr: |
          sum(rate(codeagent_requests_total{status=~"5.."}[5m])) 
          / sum(rate(codeagent_requests_total[5m])) > 0.05
        for: 5m
        labels:
          severity: critical
          team: backend
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value | humanizePercentage }} for the last 5 minutes"
          runbook: "https://wiki/runbooks/high-error-rate"

      - alert: SlowAPIResponse
        expr: |
          histogram_quantile(0.95, 
            rate(codeagent_request_duration_bucket[5m])) > 1000
        for: 10m
        labels:
          severity: warning
          team: backend
        annotations:
          summary: "API response time degraded"
          description: "95th percentile response time is {{ $value }}ms"

      - alert: WebSocketConnectionSpike
        expr: |
          rate(codeagent_websocket_connections[1m]) > 100
        for: 2m
        labels:
          severity: warning
          team: backend
        annotations:
          summary: "Unusual spike in WebSocket connections"
          description: "Connection rate is {{ $value }} per minute"
```

#### Alert Priority Matrix
| Severity | Response Time | Notification | Escalation |
|----------|--------------|--------------|------------|
| Critical | 5 minutes | PagerDuty + Slack | On-call engineer |
| High | 15 minutes | Email + Slack | Team lead |
| Medium | 30 minutes | Email | Team |
| Low | Next business day | Email digest | Team |

#### Notification Channels
- **PagerDuty**: Critical production issues
- **Slack**: Team notifications (#alerts channel)
- **Email**: Individual and digest notifications
- **SMS**: Critical after-hours alerts
- **Microsoft Teams**: Stakeholder updates

#### Acceptance Criteria
- [ ] All alert rules defined
- [ ] Notification channels configured
- [ ] Escalation policies set
- [ ] Runbooks linked
- [ ] Alert testing completed

## Testing Requirements

### Observability Testing
- Verify trace propagation
- Test metric accuracy
- Validate log correlation
- Check dashboard data
- Test alert triggers

### Performance Impact
- Measure overhead of tracing
- Test logging performance
- Validate sampling effectiveness
- Check metric collection impact

## Success Metrics
- Mean Time to Detection (MTTD): < 5 minutes
- Mean Time to Resolution (MTTR): < 30 minutes
- Dashboard load time: < 2 seconds
- Log query response: < 5 seconds
- Alert accuracy: > 95%

## Dependencies
- Monitoring infrastructure (Prometheus, Grafana)
- Log aggregation system (ELK or similar)
- APM solution (Application Insights or Datadog)
- Notification services (PagerDuty, Slack)

## Deliverables
- [ ] OpenTelemetry configuration
- [ ] Serilog integration
- [ ] Correlation ID implementation
- [ ] Grafana dashboards (5)
- [ ] Alert rule definitions
- [ ] Runbook documentation
- [ ] Monitoring guide
- [ ] Performance impact report

## Notes
- Consider adding synthetic monitoring in next phase
- Evaluate need for custom APM dashboards
- Plan for long-term metric retention
- Consider compliance requirements for log retention