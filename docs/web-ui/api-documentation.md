# CodeAgent API Documentation

## Overview
This document provides a comprehensive reference for all HTTP endpoints and WebSocket message types available in the CodeAgent Gateway API. The API consists of RESTful HTTP endpoints for traditional request-response operations and WebSocket connections for real-time bidirectional communication.

## Base URL
- Development: `http://localhost:5000`
- Production: `https://api.codeagent.com` (TBD)

## Authentication
The API uses JWT (JSON Web Token) authentication. Include the token in the Authorization header:
```
Authorization: Bearer <token>
```

For WebSocket connections, pass the token as a query parameter:
```
ws://localhost:5000/ws?access_token=<token>
```

---

## HTTP Endpoints

### Health Check

#### GET /health
Check the health status of the gateway service.

**Authentication Required:** No

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-01-10T12:00:00Z",
  "service": "CodeAgent.Gateway"
}
```

---

### Authentication Endpoints

#### POST /api/auth/register
Register a new user account.

**Authentication Required:** No

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:**
```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "ExpiresIn": 86400,
  "User": {
    "Id": "user-uuid",
    "Email": "user@example.com",
    "FirstName": "John",
    "LastName": "Doe",
    "Roles": ["user"]
  }
}
```

**Error Responses:**
- `400 Bad Request` - Email already registered
```json
{
  "error": "Email already registered"
}
```

---

#### POST /api/auth/login
Authenticate a user and receive access tokens.

**Authentication Required:** No

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "rememberMe": false
}
```

**Response:**
```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "ExpiresIn": 86400,
  "User": {
    "Id": "user-uuid",
    "Email": "user@example.com",
    "FirstName": "John",
    "LastName": "Doe",
    "Roles": ["user"]
  }
}
```

**Error Responses:**
- `401 Unauthorized` - Invalid credentials

---

#### POST /api/auth/refresh
Refresh an expired access token using a refresh token.

**Authentication Required:** No

**Request Body:**
```json
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "ExpiresIn": 86400
}
```

---

#### GET /api/auth/token
Generate a new JWT token (development endpoint).

**Authentication Required:** No

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2025-01-11T12:00:00Z"
}
```

---

### Agent Management

#### GET /api/agents
Retrieve a list of available agents.

**Authentication Required:** Yes

**Response:**
```json
[
  {
    "id": "1",
    "name": "Code Assistant",
    "type": "assistant",
    "status": "online",
    "description": "General purpose coding assistant"
  },
  {
    "id": "2",
    "name": "Test Runner",
    "type": "tester",
    "status": "offline",
    "description": "Automated test execution agent"
  }
]
```

---

### Project Management

#### GET /api/projects
Retrieve a list of user projects.

**Authentication Required:** Yes

**Response:**
```json
[
  {
    "id": "1",
    "name": "Sample Project",
    "status": "active",
    "description": "Demo project",
    "createdAt": "2025-01-03T12:00:00Z"
  }
]
```

---

### Provider Management

#### GET /api/providers
Retrieve a list of configured LLM providers.

**Authentication Required:** Yes

**Response:**
```json
[
  {
    "id": "anthropic",
    "name": "Anthropic",
    "enabled": true,
    "status": {
      "isConnected": true,
      "message": "Connected"
    },
    "models": ["claude-3-opus", "claude-3-sonnet"]
  },
  {
    "id": "openai",
    "name": "OpenAI",
    "enabled": false,
    "status": {
      "isConnected": false,
      "message": "Not configured"
    },
    "models": ["gpt-4", "gpt-3.5-turbo"]
  }
]
```

---

### Workflow Management

#### GET /api/workflows
Retrieve available workflow templates.

**Authentication Required:** Yes

**Response:**
```json
[
  {
    "id": "1",
    "name": "Code Review",
    "description": "Automated code review workflow"
  },
  {
    "id": "2",
    "name": "Test Generation",
    "description": "Generate unit tests for code"
  }
]
```

---

## WebSocket API

### Connection
Connect to the WebSocket endpoint at `/ws`.

**URL:** `ws://localhost:5000/ws`

**Authentication:** Pass JWT token as query parameter:
```
ws://localhost:5000/ws?access_token=<token>
```

### Message Format
All WebSocket messages follow this envelope format:

```json
{
  "type": "message_type",
  "payload": {
    // Message-specific data
  },
  "correlationId": "optional-correlation-id"
}
```

### Message Types

#### Client to Server Messages

##### auth
Authenticate the WebSocket connection.

**Request:**
```json
{
  "type": "auth",
  "payload": {
    "token": "jwt-token-here"
  }
}
```

**Response:**
```json
{
  "type": "auth_response",
  "success": true,
  "sessionId": "session-uuid"
}
```

---

##### chat
Send a chat message (not yet fully implemented).

**Request:**
```json
{
  "type": "chat",
  "payload": {
    "message": "User message",
    "context": {}
  }
}
```

**Response:**
```json
{
  "type": "chat_response",
  "message": "Chat functionality not yet implemented"
}
```

---

##### command
Execute a command (not yet fully implemented).

**Request:**
```json
{
  "type": "command",
  "payload": {
    "command": "command-name",
    "args": {}
  }
}
```

**Response:**
```json
{
  "type": "command_response",
  "message": "Command processing not yet implemented"
}
```

---

##### ping
Heartbeat message to keep connection alive.

**Request:**
```json
{
  "type": "ping"
}
```

**Response:**
```json
{
  "type": "pong",
  "timestamp": "2025-01-10T12:00:00Z"
}
```

---

#### Server to Client Messages

##### ping
Server-initiated heartbeat (sent every 30 seconds).

**Message:**
```json
{
  "type": "ping"
}
```

---

##### error
Error notification from server.

**Message:**
```json
{
  "type": "error",
  "message": "Error description"
}
```

---

## Error Handling

### HTTP Errors
Standard HTTP status codes are used:
- `200 OK` - Success
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required or failed
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

### WebSocket Errors
Errors are returned as messages with type "error":
```json
{
  "type": "error",
  "message": "Error description",
  "code": "ERROR_CODE"
}
```

---

## Rate Limiting
Currently not implemented. Will be added in production:
- Anonymous requests: 60 requests per hour
- Authenticated requests: 600 requests per hour
- WebSocket messages: 100 messages per minute

---

## Data Models

### RegisterRequest
```typescript
{
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}
```

### LoginRequest
```typescript
{
  email: string;
  password: string;
  rememberMe?: boolean;
}
```

### RefreshTokenRequest
```typescript
{
  refreshToken: string;
}
```

### AuthResponse
```typescript
{
  Token: string;
  RefreshToken: string;
  ExpiresIn: number;  // seconds
  User?: UserDto;
}
```

### UserDto
```typescript
{
  Id: string;
  Email: string;
  FirstName: string;
  LastName: string;
  Roles?: string[];
}
```

---

## Future Endpoints (Planned)

### User Management
- `GET /api/user/profile` - Get user profile
- `PUT /api/user/profile` - Update user profile
- `POST /api/user/change-password` - Change password
- `POST /api/user/reset-password` - Request password reset
- `POST /api/user/verify-email` - Verify email address

### Agent Operations
- `POST /api/agents/{id}/start` - Start an agent
- `POST /api/agents/{id}/stop` - Stop an agent
- `GET /api/agents/{id}/status` - Get agent status
- `POST /api/agents/{id}/execute` - Execute agent command

### Project Operations
- `POST /api/projects` - Create new project
- `GET /api/projects/{id}` - Get project details
- `PUT /api/projects/{id}` - Update project
- `DELETE /api/projects/{id}` - Delete project
- `GET /api/projects/{id}/files` - List project files

### WebSocket Message Types (Planned)
- `agent_start` - Start agent session
- `agent_response` - Agent response stream
- `file_update` - File change notification
- `project_sync` - Project synchronization
- `notification` - System notifications

---

## Notes

1. **Development Status**: This API is currently in active development. Many endpoints return mock data and WebSocket handlers are partially implemented.

2. **Authentication**: The current implementation uses in-memory storage for users. Production will use a proper database.

3. **CORS**: Currently configured to allow all origins. This will be restricted in production.

4. **WebSocket Protocol**: The WebSocket implementation includes automatic reconnection and heartbeat monitoring.

5. **Token Expiration**: Access tokens expire after 24 hours. Use the refresh endpoint to obtain new tokens.

---

## Version History

- **v0.1.0** (Current) - Initial API implementation with basic auth and mock endpoints
- **v0.2.0** (Planned) - Full agent integration and project management
- **v0.3.0** (Planned) - WebSocket streaming and real-time updates
- **v1.0.0** (Planned) - Production-ready API with full feature set