# API Architecture

## Rate Limiting

### Headers
All API responses include rate limit information:
- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Requests remaining in window
- `X-RateLimit-Reset`: Unix timestamp when limit resets

### Tier Limits

| Tier | Requests/Hour | Concurrent Sessions | Context Size |
|------|---------------|---------------------|--------------|
| Free | 100 | 1 | 32k |
| Pro | 1000 | 5 | 128k |
| Enterprise | Unlimited | Unlimited | 200k |

## API Versioning

### Version Strategy
- URL versioning (e.g., `/v1/`, `/v2/`)
- Current version: `v1`
- Version specified in URL path

### Deprecation Policy
- New versions announced 6 months before release
- Previous version supported for 12 months after new release
- Deprecation warnings in response headers

### Deprecation Headers
- `X-API-Deprecation-Date`: Date when API version will be deprecated
- `X-API-Deprecation-Info`: URL to migration documentation

## SDK Support

### Official SDKs
- C# SDK via NuGet package
- JavaScript/TypeScript SDK via npm
- Python SDK via pip (planned)
- Go SDK via go modules (planned)

### SDK Features
- Automatic retry with backoff
- Rate limit handling
- WebSocket connection management
- Type-safe request/response models
- Async/await support

## Health & Monitoring

### Health Endpoints
- `/health` - Basic health check
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe
- `/metrics` - Prometheus metrics

### Health Response
```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "services": {
    "gateway": "up",
    "docker": "up",
    "database": "up"
  },
  "version": "1.0.0"
}
```

## Error Handling

### Error Response Format
All errors follow consistent structure:
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human readable message",
    "details": {},
    "timestamp": "2024-01-01T00:00:00Z",
    "traceId": "trace-123"
  }
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| AUTH_FAILED | 401 | Authentication failed |
| UNAUTHORIZED | 403 | Not authorized |
| NOT_FOUND | 404 | Resource not found |
| VALIDATION_ERROR | 400 | Request validation failed |
| CONTEXT_LIMIT_EXCEEDED | 413 | Context window exceeded |
| RATE_LIMIT_EXCEEDED | 429 | Too many requests |
| PROVIDER_ERROR | 502 | Provider service error |
| INTERNAL_ERROR | 500 | Internal server error |

## Authentication

### JWT Tokens
- Bearer token authentication
- Token expiration: 1 hour default
- Refresh token support
- Token rotation on refresh

### API Keys
- Alternative to JWT for programmatic access
- No expiration by default
- Scoped permissions
- Revocable

## WebSocket Protocol

### Connection Flow
1. Connect to `/ws/v1/connect`
2. Send authentication message
3. Receive acknowledgment
4. Begin message exchange
5. Handle heartbeat/keepalive

### Message Types
- `auth` - Authentication
- `command` - User command
- `response` - Command response
- `stream` - Streaming response
- `status` - Status update
- `error` - Error message
- `heartbeat` - Keepalive