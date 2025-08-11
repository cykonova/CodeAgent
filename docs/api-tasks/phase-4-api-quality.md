# Phase 4: API Quality & Standards
**Duration**: Week 6  
**Priority**: MEDIUM  
**Dependencies**: Phase 3 (Observability)

## Overview
Enhance API quality through versioning, comprehensive documentation, rate limiting, and standardization. This phase ensures the API is developer-friendly, scalable, and maintains backward compatibility.

## Tasks

### 4.1 API Versioning Implementation (2 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Implement URL path versioning
- Support header-based versioning
- Create version negotiation
- Handle deprecated versions
- Document breaking changes

#### Versioning Strategy
```csharp
// In Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-API-Version"),
        new MediaTypeApiVersionReader("version")
    );
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Controllers with versioning
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProvidersController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetV1()
    {
        // V1 implementation
        return Ok(new { version = "1.0", providers = GetBasicProviders() });
    }

    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> GetV2()
    {
        // V2 implementation with enhanced features
        return Ok(new { version = "2.0", providers = GetDetailedProviders() });
    }
}
```

#### Version Deprecation Policy
```csharp
public class ApiVersionDeprecationMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var apiVersion = context.GetRequestedApiVersion();
        
        var deprecationInfo = GetDeprecationInfo(apiVersion);
        if (deprecationInfo != null)
        {
            context.Response.Headers.Add("X-API-Deprecation-Date", 
                deprecationInfo.Date.ToString("yyyy-MM-dd"));
            context.Response.Headers.Add("X-API-Deprecation-Info", 
                deprecationInfo.Message);
            context.Response.Headers.Add("X-API-Latest-Version", 
                GetLatestVersion().ToString());
        }
        
        await _next(context);
    }
}
```

#### API Version Matrix
| Version | Status | Released | Deprecated | Sunset | Notes |
|---------|--------|----------|------------|--------|-------|
| v1.0 | Active | 2024-01 | 2024-07 | 2024-10 | Initial release |
| v1.1 | Active | 2024-03 | 2024-09 | 2024-12 | Bug fixes |
| v2.0 | Active | 2024-06 | - | - | Breaking changes |
| v3.0 | Beta | 2024-09 | - | - | New architecture |

#### Acceptance Criteria
- [ ] URL path versioning working
- [ ] Header versioning supported
- [ ] Version negotiation implemented
- [ ] Deprecation headers added
- [ ] Version documentation complete

### 4.2 Comprehensive Swagger Documentation (2 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Document all endpoints
- Add request/response examples
- Include authentication flows
- Generate client SDKs
- Create interactive documentation

#### Enhanced Swagger Configuration
```csharp
builder.Services.AddSwaggerGen(options =>
{
    // Add versions
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "CodeAgent API",
        Description = "API for managing AI agents and workflows",
        TermsOfService = new Uri("https://codeagent.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "api@codeagent.com",
            Url = new Uri("https://codeagent.com/support")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add security definitions
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Add XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add examples
    options.ExampleFilters();
    
    // Add operation filters
    options.OperationFilter<ApiVersionOperationFilter>();
    options.OperationFilter<ResponseHeadersFilter>();
    
    // Custom schema filters
    options.SchemaFilter<EnumSchemaFilter>();
    options.SchemaFilter<RequiredPropertiesSchemaFilter>();
});

// Add Swashbuckle examples
services.AddSwaggerExamplesFromAssemblyOf<Startup>();
```

#### API Documentation Example
```csharp
/// <summary>
/// Creates a new agent configuration
/// </summary>
/// <param name="request">Agent creation request</param>
/// <returns>Created agent details</returns>
/// <response code="201">Agent created successfully</response>
/// <response code="400">Invalid request data</response>
/// <response code="409">Agent with same name already exists</response>
[HttpPost]
[ProducesResponseType(typeof(AgentResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
public async Task<IActionResult> CreateAgent([FromBody] CreateAgentRequest request)
{
    // Implementation
}

public class CreateAgentRequestExample : IExamplesProvider<CreateAgentRequest>
{
    public CreateAgentRequest GetExamples()
    {
        return new CreateAgentRequest
        {
            Name = "Code Review Agent",
            Description = "Automated code review assistant",
            Provider = "anthropic",
            Model = "claude-3-opus",
            SystemPrompt = "You are a code review expert...",
            Temperature = 0.3,
            MaxTokens = 4000,
            Tags = new[] { "development", "quality" }
        };
    }
}
```

#### OpenAPI Extensions
```yaml
paths:
  /api/v1/agents:
    post:
      x-code-samples:
        - lang: 'C#'
          source: |
            var client = new CodeAgentClient(apiKey);
            var agent = await client.Agents.CreateAsync(new CreateAgentRequest
            {
                Name = "My Agent",
                Provider = "anthropic"
            });
        - lang: 'Python'
          source: |
            client = CodeAgentClient(api_key)
            agent = client.agents.create(
                name="My Agent",
                provider="anthropic"
            )
        - lang: 'JavaScript'
          source: |
            const client = new CodeAgentClient(apiKey);
            const agent = await client.agents.create({
                name: 'My Agent',
                provider: 'anthropic'
            });
```

#### Acceptance Criteria
- [ ] All endpoints documented
- [ ] Request/response examples added
- [ ] Authentication documented
- [ ] Code samples included
- [ ] Interactive UI working
- [ ] SDK generation configured

### 4.3 Rate Limiting Implementation (2 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Implement sliding window rate limiting
- Add per-user and per-IP limits
- Configure endpoint-specific limits
- Add rate limit headers
- Implement burst handling

#### Rate Limiting Configuration
```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Specific limits for expensive operations
    options.AddPolicy("expensive", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    // Token bucket for burst handling
    options.AddPolicy("burst", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
            factory: partition => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokensPerPeriod = 5,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    // Add response headers
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.Add("X-Rate-Limit-Limit", 
            context.Lease.TotalCount.ToString());
        context.HttpContext.Response.Headers.Add("X-Rate-Limit-Remaining", "0");
        context.HttpContext.Response.Headers.Add("X-Rate-Limit-Reset", 
            DateTimeOffset.UtcNow.Add(context.Lease.ReplenishmentPeriod).ToUnixTimeSeconds().ToString());

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded",
            retryAfter = context.Lease.ReplenishmentPeriod.TotalSeconds
        });
    };
});

app.UseRateLimiter();
```

#### Rate Limit Tiers
| Tier | Requests/Min | Burst | Concurrent | Monthly Quota |
|------|-------------|-------|------------|---------------|
| Free | 20 | 5 | 2 | 10,000 |
| Basic | 60 | 15 | 5 | 100,000 |
| Pro | 300 | 50 | 20 | 1,000,000 |
| Enterprise | Custom | Custom | Custom | Unlimited |

#### Per-Endpoint Limits
```csharp
[HttpPost("execute")]
[EnableRateLimiting("expensive")]
public async Task<IActionResult> ExecuteWorkflow([FromBody] WorkflowRequest request)
{
    // Expensive operation
}

[HttpGet]
[EnableRateLimiting("burst")]
public async Task<IActionResult> GetProviders()
{
    // Allow bursts for reads
}
```

#### Acceptance Criteria
- [ ] Sliding window rate limiting
- [ ] Per-user limits working
- [ ] Endpoint-specific limits
- [ ] Rate limit headers present
- [ ] 429 responses correct
- [ ] Burst handling works

### 4.4 Request/Response Validation (1 day)
**Owner**: Backend Team  
**Complexity**: Low

#### Requirements
- Validate all request payloads
- Implement response validation
- Add schema validation
- Create validation middleware
- Standardize error responses

#### Validation Pipeline
```csharp
public class ValidationPipelineBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        var response = await next();
        
        // Validate response if needed
        if (response is IValidatable validatable)
        {
            validatable.Validate();
        }
        
        return response;
    }
}
```

#### Complex Validation Rules
```csharp
public class WorkflowRequestValidator : AbstractValidator<WorkflowRequest>
{
    public WorkflowRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9-_]+$")
            .WithMessage("Name can only contain alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.Steps)
            .NotEmpty()
            .Must(steps => steps.Count <= 50)
            .WithMessage("Workflow cannot have more than 50 steps");

        RuleForEach(x => x.Steps)
            .SetValidator(new WorkflowStepValidator());

        RuleFor(x => x)
            .Must(HaveValidDependencies)
            .WithMessage("Workflow contains invalid step dependencies");

        RuleFor(x => x.Timeout)
            .InclusiveBetween(1, 3600)
            .When(x => x.Timeout.HasValue);
    }

    private bool HaveValidDependencies(WorkflowRequest request)
    {
        var stepIds = request.Steps.Select(s => s.Id).ToHashSet();
        return request.Steps
            .Where(s => s.DependsOn != null)
            .All(s => s.DependsOn.All(dep => stepIds.Contains(dep)));
    }
}
```

#### Response Validation
```csharp
public class ResponseValidationMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        if (context.Response.StatusCode == 200)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            
            // Validate response structure
            if (!IsValidJson(text))
            {
                _logger.LogError("Invalid JSON response detected");
                // Handle invalid response
            }
            
            context.Response.Body.Seek(0, SeekOrigin.Begin);
        }

        await responseBody.CopyToAsync(originalBodyStream);
    }
}
```

#### Acceptance Criteria
- [ ] All requests validated
- [ ] Response validation active
- [ ] Schema validation working
- [ ] Consistent error format
- [ ] Validation metrics tracked

### 4.5 API Standards & Guidelines (1 day)
**Owner**: Backend Team  
**Complexity**: Low

#### Requirements
- Define REST standards
- Create naming conventions
- Establish error standards
- Document best practices
- Create API style guide

#### REST API Standards
```yaml
# API Design Standards

## URL Structure
- Use nouns for resources: /api/v1/agents
- Use verbs for actions: /api/v1/agents/{id}/execute
- Use kebab-case for multi-word: /api/v1/workflow-templates
- Use query params for filtering: /api/v1/agents?status=active

## HTTP Methods
- GET: Retrieve resources (idempotent)
- POST: Create new resources
- PUT: Full update (idempotent)
- PATCH: Partial update
- DELETE: Remove resources (idempotent)

## Status Codes
- 200 OK: Successful GET, PUT, PATCH
- 201 Created: Successful POST
- 202 Accepted: Async operation started
- 204 No Content: Successful DELETE
- 400 Bad Request: Validation errors
- 401 Unauthorized: Missing/invalid auth
- 403 Forbidden: Insufficient permissions
- 404 Not Found: Resource doesn't exist
- 409 Conflict: Resource already exists
- 429 Too Many Requests: Rate limited
- 500 Internal Server Error: Server fault
- 503 Service Unavailable: Temporary outage

## Response Format
{
  "data": { ... },           // Actual response data
  "meta": {                  // Metadata
    "timestamp": "...",
    "version": "1.0",
    "correlationId": "..."
  },
  "pagination": {            // For list endpoints
    "page": 1,
    "pageSize": 20,
    "totalPages": 5,
    "totalCount": 100
  },
  "links": {                 // HATEOAS links
    "self": "...",
    "next": "...",
    "prev": "..."
  }
}

## Error Format
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": [ ... ],
    "timestamp": "...",
    "correlationId": "...",
    "help": "https://docs.codeagent.com/errors/VALIDATION_ERROR"
  }
}
```

#### Naming Conventions
| Resource | Endpoint | Example |
|----------|----------|---------|
| Collection | /resources | /api/v1/agents |
| Single Item | /resources/{id} | /api/v1/agents/123 |
| Sub-resource | /resources/{id}/sub | /api/v1/agents/123/executions |
| Action | /resources/{id}/action | /api/v1/agents/123/execute |
| Search | /resources/search | /api/v1/agents/search?q=code |

#### Pagination Standards
```csharp
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; }
    public PaginationMeta Pagination { get; set; }
    public Dictionary<string, string> Links { get; set; }
}

[HttpGet]
public async Task<IActionResult> GetAgents(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string sortBy = "createdAt",
    [FromQuery] string sortOrder = "desc")
{
    // Implementation
}
```

#### Acceptance Criteria
- [ ] REST standards documented
- [ ] Naming conventions defined
- [ ] Error standards established
- [ ] Style guide created
- [ ] Team trained on standards

## Testing Requirements

### API Contract Testing
- Schema validation tests
- Version compatibility tests
- Breaking change detection
- Response format validation

### Performance Testing
- Rate limit verification
- Documentation generation time
- Validation performance impact
- Version negotiation overhead

## Success Metrics
- API documentation completeness: 100%
- Breaking changes per release: 0
- Rate limit effectiveness: 99%
- Validation coverage: 100%
- Standards compliance: 95%

## Dependencies
- API versioning packages
- Swagger/OpenAPI tools
- Rate limiting middleware
- Validation frameworks

## Deliverables
- [ ] API versioning implementation
- [ ] Complete Swagger documentation
- [ ] Rate limiting configuration
- [ ] Validation pipeline
- [ ] API standards document
- [ ] Client SDK generators
- [ ] Developer portal
- [ ] Migration guide for versions

## Notes
- Consider GraphQL for complex querying needs
- Evaluate gRPC for internal service communication
- Plan for webhook support in future
- Consider API gateway for advanced features