# Code Agent API Specification

## Version 1.0

### Base URL
```
https://api.codeagent.local/v1
wss://api.codeagent.local/ws/v1
```

## Authentication

### JWT Authentication
All API requests must include a valid JWT token in the Authorization header.

```http
Authorization: Bearer <jwt-token>
```

### Token Request
```http
POST /auth/token
Content-Type: application/json

{
  "username": "user@example.com",
  "password": "secure-password",
  "grant_type": "password"
}
```

**Response:**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "8xLOxBtZp8..."
}
```

### Token Refresh
```http
POST /auth/refresh
Content-Type: application/json

{
  "refresh_token": "8xLOxBtZp8..."
}
```

## WebSocket Protocol

### Connection Establishment
```javascript
const ws = new WebSocket('wss://api.codeagent.local/ws/v1/connect');

// Send authentication immediately after connection
ws.onopen = () => {
  ws.send(JSON.stringify({
    type: 'auth',
    token: 'Bearer eyJhbGciOiJIUzI1NiIs...'
  }));
};
```

### Message Types

#### Command Message
```json
{
  "id": "msg-123",
  "type": "command",
  "timestamp": "2024-01-01T00:00:00Z",
  "payload": {
    "command": "/plan",
    "args": ["microservice", "authentication"],
    "prompt": "Design an authentication microservice with OAuth2 support",
    "context": {
      "projectId": "proj-456",
      "files": ["src/main.cs", "src/models/user.cs"],
      "language": "csharp"
    }
  }
}
```

#### Response Message
```json
{
  "id": "resp-123",
  "type": "response",
  "correlationId": "msg-123",
  "timestamp": "2024-01-01T00:00:01Z",
  "payload": {
    "status": "success",
    "data": {
      "content": "Here's the authentication microservice design...",
      "artifacts": ["architecture.md", "api-spec.yaml"],
      "suggestions": ["Consider implementing rate limiting", "Add 2FA support"]
    }
  }
}
```

#### Stream Message
```json
{
  "id": "stream-123",
  "type": "stream",
  "correlationId": "msg-123",
  "sequence": 1,
  "payload": {
    "chunk": "Creating authentication service...",
    "isComplete": false
  }
}
```

#### Status Message
```json
{
  "id": "status-123",
  "type": "status",
  "payload": {
    "status": "processing",
    "message": "Analyzing codebase...",
    "progress": 45
  }
}
```

#### Error Message
```json
{
  "id": "err-123",
  "type": "error",
  "correlationId": "msg-123",
  "payload": {
    "code": "CONTEXT_LIMIT_EXCEEDED",
    "message": "The context window limit has been exceeded",
    "details": {
      "limit": 128000,
      "requested": 145000
    }
  }
}
```

### WebSocket Commands

#### Available Commands
| Command | Description | Arguments | Example |
|---------|-------------|-----------|---------|
| `/plan` | Create project plan | `[type] [description]` | `/plan webapp "E-commerce platform"` |
| `/code` | Generate code | `[type] [specification]` | `/code function "Calculate fibonacci"` |
| `/review` | Review code | `[files...]` | `/review src/main.cs src/utils.cs` |
| `/test` | Generate tests | `[files...]` | `/test src/services/auth.cs` |
| `/docs` | Generate documentation | `[type] [files...]` | `/docs api src/controllers/` |
| `/refactor` | Refactor code | `[pattern] [files...]` | `/refactor solid src/legacy/` |
| `/explain` | Explain code | `[file:line]` | `/explain src/main.cs:45` |
| `/fix` | Fix issues | `[error or file]` | `/fix "NullReferenceException"` |
| `/optimize` | Optimize code | `[metric] [files...]` | `/optimize performance src/` |
| `/search` | Search codebase | `[query]` | `/search "database connection"` |

## REST API Endpoints

### Provider Management

#### List All Supported Providers
```http
GET /providers
```

**Response:**
```json
{
  "providers": [
    {
      "id": "anthropic",
      "name": "Anthropic Claude",
      "type": "api",
      "status": "connected",
      "models": [
        {
          "id": "claude-3-opus-20240229",
          "name": "Claude 3 Opus",
          "contextWindow": 200000,
          "pricing": { "input": 0.015, "output": 0.075 }
        },
        {
          "id": "claude-3-sonnet-20240229",
          "name": "Claude 3 Sonnet",
          "contextWindow": 200000,
          "pricing": { "input": 0.003, "output": 0.015 }
        }
      ]
    },
    {
      "id": "openai",
      "name": "OpenAI",
      "type": "api",
      "status": "connected",
      "models": [
        {
          "id": "gpt-4-turbo-preview",
          "name": "GPT-4 Turbo",
          "contextWindow": 128000
        },
        {
          "id": "gpt-4",
          "name": "GPT-4",
          "contextWindow": 8192
        }
      ]
    },
    {
      "id": "gemini",
      "name": "Google Gemini",
      "type": "api",
      "status": "not_configured",
      "models": []
    },
    {
      "id": "ollama",
      "name": "Ollama (Local)",
      "type": "local",
      "status": "connected",
      "models": [
        {
          "id": "codellama:34b",
          "name": "Code Llama 34B",
          "contextWindow": 16384
        }
      ]
    }
  ]
}
```

#### Configure Provider
```http
PUT /providers/{providerId}/config
Content-Type: application/json

{
  "apiKey": "sk-...",
  "endpoint": "https://api.anthropic.com",
  "enabled": true
}
```

#### Test Provider Connection
```http
POST /providers/{providerId}/test
```

**Response:**
```json
{
  "status": "success",
  "latency": 245,
  "available_models": ["claude-3-opus", "claude-3-sonnet"],
  "message": "Provider connected successfully"
}
```

### Project Management

#### List Projects
```http
GET /projects
```

**Response:**
```json
{
  "projects": [
    {
      "id": "proj-123",
      "name": "E-commerce API",
      "description": "RESTful API for e-commerce platform",
      "language": "csharp",
      "framework": "aspnetcore",
      "created": "2024-01-01T00:00:00Z",
      "lastModified": "2024-01-02T00:00:00Z"
    }
  ]
}
```

#### Create Project with Configuration
```http
POST /projects
Content-Type: application/json

{
  "name": "New Project",
  "description": "Project description",
  "language": "csharp",
  "framework": "aspnetcore",
  "repository": "https://github.com/user/repo",
  "workflowTemplate": "default",
  "providerPreferences": {
    "primary": "anthropic",
    "fallback": ["openai", "gemini"],
    "preferLocal": false
  }
}
```

**Response:**
```json
{
  "id": "proj-456",
  "name": "New Project",
  "workflowConfig": {
    "path": "projects/proj-456/workflow.yaml",
    "inherited_from": "default"
  },
  "agentAssignments": {
    "planning": "anthropic/claude-3-opus",
    "coding": "openai/gpt-4-turbo",
    "review": "anthropic/claude-3-sonnet",
    "testing": "gemini/gemini-pro",
    "documentation": "mistral/mistral-large"
  }
}
```

#### Get Project Details
```http
GET /projects/{projectId}
```

#### Update Project
```http
PUT /projects/{projectId}
Content-Type: application/json

{
  "name": "Updated Name",
  "description": "Updated description"
}
```

#### Delete Project
```http
DELETE /projects/{projectId}
```

### Agent Configuration

#### Get Agent Configuration
```http
GET /agents/config
```

**Response:**
```json
{
  "agents": {
    "planning": {
      "name": "Planning Agent",
      "description": "Analyzes requirements and creates designs",
      "provider": "anthropic",
      "model": "claude-3-opus-20240229",
      "settings": {
        "temperature": 0.7,
        "maxTokens": 4096
      },
      "fallbackProviders": ["openai/gpt-4", "gemini/gemini-ultra"]
    },
    "coding": {
      "name": "Coding Agent",
      "provider": "openai",
      "model": "gpt-4-turbo-preview",
      "settings": {
        "temperature": 0.3,
        "maxTokens": 8192
      }
    }
  }
}
```

#### Update Agent Configuration
```http
PUT /agents/{agentType}/config
Content-Type: application/json

{
  "provider": "gemini",
  "model": "gemini-ultra",
  "settings": {
    "temperature": 0.5,
    "maxTokens": 8192
  },
  "fallbackProviders": ["anthropic/claude-3-opus"]
}
```

#### Execute Agent Task with Provider Override
```http
POST /agents/{agentType}/execute
Content-Type: application/json

{
  "projectId": "proj-123",
  "task": "Generate REST API",
  "providerOverride": {
    "provider": "openai",
    "model": "gpt-4"
  },
  "context": {
    "files": ["models/user.cs"],
    "requirements": ["OAuth2", "CRUD operations"]
  }
}
```

### Context Management

#### Get Context Window Status
```http
GET /context/{sessionId}/status
```

**Response:**
```json
{
  "sessionId": "sess-789",
  "totalTokens": 128000,
  "usedTokens": 45000,
  "segments": {
    "system": 6400,
    "project": 19200,
    "history": 15000,
    "current": 4400,
    "buffer": 12800
  }
}
```

#### Clear Context
```http
POST /context/{sessionId}/clear
```

#### Add to Context
```http
POST /context/{sessionId}/add
Content-Type: application/json

{
  "type": "file",
  "content": "public class UserService { ... }",
  "priority": "high"
}
```

### Workflow Management

#### Get Default Workflow
```http
GET /workflows/default
```

**Response:**
```json
{
  "name": "Standard Development Workflow",
  "version": "1.0",
  "stages": [
    {
      "name": "Requirements Analysis",
      "agent": "planning",
      "provider": "anthropic",
      "model": "claude-3-opus-20240229"
    },
    {
      "name": "Implementation",
      "agent": "coding",
      "provider": "openai",
      "model": "gpt-4-turbo-preview"
    },
    {
      "name": "Review",
      "agent": "review",
      "provider": "anthropic",
      "model": "claude-3-sonnet-20240229"
    }
  ]
}
```

#### Create Custom Workflow
```http
POST /workflows
Content-Type: application/json

{
  "name": "Fast Development",
  "baseWorkflow": "default",
  "overrides": {
    "planning": {
      "provider": "gemini",
      "model": "gemini-pro"
    },
    "coding": {
      "provider": "ollama",
      "model": "codellama:34b"
    }
  }
}
```

#### Get Project Workflow
```http
GET /projects/{projectId}/workflow
```

#### Update Project Workflow
```http
PUT /projects/{projectId}/workflow
Content-Type: application/json

{
  "baseWorkflow": "default",
  "agentOverrides": {
    "planning": {
      "provider": "anthropic",
      "model": "claude-3-opus-20240229"
    },
    "coding": {
      "provider": "openai",
      "model": "gpt-4"
    }
  },
  "costLimit": 25.00,
  "preferLocalModels": true
}
```

### Docker Sandbox Management

#### Create Project Sandbox
```http
POST /projects/{projectId}/sandbox
Content-Type: application/json

{
  "enabled": true,
  "image": "codeagent/sandbox:latest",
  "securityLevel": "development",
  "resources": {
    "memory": "4G",
    "cpus": 2,
    "gpus": 0
  },
  "volumes": [
    {
      "source": "./src",
      "target": "/workspace/src",
      "readOnly": false
    }
  ],
  "environment": {
    "NODE_ENV": "development",
    "DEBUG": "true"
  }
}
```

**Response:**
```json
{
  "sandboxId": "sandbox-789",
  "containerId": "abc123def456",
  "status": "running",
  "endpoint": "http://sandbox-789:8500",
  "created": "2024-01-01T00:00:00Z"
}
```

#### Execute MCP Command in Sandbox
```http
POST /projects/{projectId}/sandbox/execute
Content-Type: application/json

{
  "tool": "exec",
  "parameters": {
    "command": "npm",
    "args": ["test"]
  }
}
```

**Response:**
```json
{
  "success": true,
  "output": "Test results...",
  "exitCode": 0,
  "duration": 1250
}
```

#### Get Sandbox Status
```http
GET /projects/{projectId}/sandbox/status
```

**Response:**
```json
{
  "status": "running",
  "uptime": 3600,
  "resources": {
    "cpuUsage": 25.5,
    "memoryUsage": 1073741824,
    "memoryLimit": 4294967296,
    "diskUsage": 536870912
  },
  "lastActivity": "2024-01-01T00:30:00Z"
}
```

#### Stop/Start Sandbox
```http
POST /projects/{projectId}/sandbox/stop
POST /projects/{projectId}/sandbox/start
```

#### Stream Sandbox Logs
```http
GET /projects/{projectId}/sandbox/logs
Accept: text/event-stream
```

**Response (SSE):**
```
data: {"timestamp": "2024-01-01T00:00:00Z", "message": "Starting npm install..."}
data: {"timestamp": "2024-01-01T00:00:01Z", "message": "Installing dependencies..."}
```

### Docker MCP Configuration

#### Configure Docker Desktop MCP
```http
POST /mcp/docker/configure
Content-Type: application/json

{
  "enabled": true,
  "socketPath": "/var/run/docker.sock",
  "capabilities": [
    "container_management",
    "image_management",
    "compose_orchestration"
  ]
}
```

#### List Available MCP Tools
```http
GET /mcp/tools
```

**Response:**
```json
{
  "tools": [
    {
      "name": "docker.container.create",
      "description": "Create a new container",
      "parameters": {
        "image": "string",
        "name": "string",
        "env": "array"
      }
    },
    {
      "name": "docker.compose.up",
      "description": "Start services via docker-compose",
      "parameters": {
        "file": "string",
        "detached": "boolean"
      }
    },
    {
      "name": "sandbox.exec",
      "description": "Execute command in project sandbox",
      "parameters": {
        "command": "string",
        "args": "array"
      }
    }
  ]
}
```

#### Configure Docker LLM Provider
```http
POST /providers/docker-llm/configure
Content-Type: application/json

{
  "enabled": true,
  "models": [
    {
      "id": "llama3-container",
      "image": "docker-llm/llama3:latest",
      "resources": {
        "memory": "8G",
        "cpus": 4,
        "gpus": 1
      }
    }
  ]
}
```

## Error Responses

### Error Response Format
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human readable error message",
    "details": {
      "field": "Additional context"
    },
    "timestamp": "2024-01-01T00:00:00Z",
    "traceId": "trace-123"
  }
}
```

### Error Codes
| Code | HTTP Status | Description |
|------|-------------|-------------|
| `AUTH_FAILED` | 401 | Authentication failed |
| `UNAUTHORIZED` | 403 | Not authorized for resource |
| `NOT_FOUND` | 404 | Resource not found |
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `CONTEXT_LIMIT_EXCEEDED` | 413 | Context window limit exceeded |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `PROVIDER_ERROR` | 502 | Provider service error |
| `INTERNAL_ERROR` | 500 | Internal server error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |

## Rate Limiting

### Headers
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640995200
```

### Limits
| Tier | Requests/Hour | Concurrent Sessions | Context Size |
|------|---------------|---------------------|--------------|
| Free | 100 | 1 | 32k |
| Pro | 1000 | 5 | 128k |
| Enterprise | Unlimited | Unlimited | 200k |

### Plugin Management

#### List Installed Plugins
```http
GET /plugins
```

**Response:**
```json
{
  "plugins": [
    {
      "id": "docker-mcp-tools",
      "name": "Docker MCP Tools",
      "version": "1.0.0",
      "type": "mcp-provider",
      "status": "active",
      "permissions": ["docker.access", "filesystem.read"]
    },
    {
      "id": "docker-llm-provider",
      "name": "Docker LLM Provider",
      "version": "0.9.0-beta",
      "type": "llm-provider",
      "status": "active"
    }
  ]
}
```

#### Install Plugin
```http
POST /plugins/install
Content-Type: application/json

{
  "source": "registry|file|url",
  "name": "docker-mcp-tools",
  "version": "latest"
}
```

#### Configure Plugin
```http
PUT /plugins/{pluginId}/config
Content-Type: application/json

{
  "docker_socket": "/var/run/docker.sock",
  "enable_compose": true,
  "enable_swarm": false
}
```

#### Enable/Disable Plugin
```http
POST /plugins/{pluginId}/enable
POST /plugins/{pluginId}/disable
```

## SDK Examples

### C# SDK Usage
```csharp
using CodeAgent.SDK;

var client = new CodeAgentClient("api-key");

// Execute command
var response = await client.ExecuteCommandAsync(new CommandRequest
{
    Command = "/plan",
    Args = new[] { "microservice", "authentication" },
    ProjectId = "proj-123"
});

// Stream response
await foreach (var chunk in client.StreamCommandAsync(request))
{
    Console.Write(chunk.Content);
}
```

### JavaScript SDK Usage
```javascript
import { CodeAgentClient } from '@codeagent/sdk';

const client = new CodeAgentClient({ apiKey: 'api-key' });

// WebSocket connection
const session = await client.connect();

session.on('message', (msg) => {
  console.log('Received:', msg);
});

// Send command
await session.sendCommand({
  command: '/code',
  args: ['function', 'fibonacci'],
  context: { language: 'javascript' }
});
```

## Versioning

The API uses URL versioning. The current version is `v1`.

### Deprecation Policy
- New versions announced 6 months before release
- Previous version supported for 12 months after new release
- Deprecation warnings included in headers

```http
X-API-Deprecation-Date: 2025-01-01
X-API-Deprecation-Info: https://docs.codeagent.com/migration
```

---
*Specification Version: 1.0*  
*Last Updated: [Current Date]*  
*OpenAPI Spec: https://api.codeagent.local/v1/openapi.json*