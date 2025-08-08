# CodeAgent Architecture Overview

## Project Type
CodeAgent is a production-ready web-based LLM integration platform built with .NET 8 and Angular 20, following Clean Architecture and Domain-Driven Design principles.

## Solution Structure

### Domain Layer (CodeAgent.Domain)
- **Purpose**: Core business logic and abstractions with zero external dependencies
- **Key Components**:
  - Interfaces: ILLMProvider, IChatService, IFileSystemService, IGitService, ISecurityService
  - Models: ChatMessage, ChatRequest, ChatResponse, Session, ConfigurationProfile
  - Security: Role, SecurityPolicy, AuditEntry, DLP models, Sandbox models, Threat models
  - Configuration: LLMProviderSettings
  - MCP: MCPTool, MCPParameter, MCPContext, MCPToolResult

### Core Layer (CodeAgent.Core)
- **Purpose**: Application services and use cases
- **Key Services**:
  - ChatService: Orchestrates chat interactions with LLM providers
  - ProviderManager: Manages multiple LLM provider instances
  - LLMProviderFactory: Creates provider instances based on configuration
  - ContextManager/ContextService: Manages conversation context
  - ProfileService: Handles configuration profiles
  - PluginManager: Extensibility through plugins
  - InternalToolService: Built-in tool execution

### Infrastructure Layer (CodeAgent.Infrastructure)
- **Purpose**: External service implementations
- **Key Services**:
  - FileSystemService: File I/O with change tracking
  - GitService: LibGit2Sharp integration for version control
  - SecurityService: Security policy enforcement
  - DlpService: Data Loss Prevention scanning
  - SandboxService: Sandboxed code execution
  - ThreatDetectionService: Security threat analysis
  - ConfigurationService: Persistent configuration management
  - CacheService: Response caching
  - AuditService: Audit logging

### Provider Layer (CodeAgent.Providers)
- **Implementations**:
  - CodeAgent.Providers.OpenAI: GPT models via OpenAI API
  - CodeAgent.Providers.Claude: Anthropic Claude models
  - CodeAgent.Providers.Ollama: Local model hosting
  - CodeAgent.Providers.Docker: Containerized model execution
- **Features**: Streaming responses, tool calling, retry logic, connection validation

### MCP Layer (CodeAgent.MCP)
- **Purpose**: Model Context Protocol implementation
- **Components**:
  - MCPClient: Protocol client implementation
  - DockerMCPProvider: Docker-based MCP support
  - MCPOptions: Configuration for MCP connections

### Web Layer (CodeAgent.Web)
- **Backend (ASP.NET Core 8)**:
  - Controllers: Chat, Configuration, File, Models, Security, Health
  - SignalR Hub: ChatHub for real-time streaming
  - Program.cs: Dependency injection and middleware configuration
- **Frontend (Angular 20)**:
  - Components: Chat interface, Configuration panel, File browser
  - Services: ChatService (SignalR), ConfigurationService, FileService
  - Material Design UI components
  - Responsive layout with sidebar navigation

## Key Architectural Decisions

### Clean Architecture
- Dependency inversion: Inner layers don't depend on outer layers
- Domain layer has no external dependencies
- Infrastructure and UI depend on Domain abstractions

### Communication Patterns
- REST API for CRUD operations
- SignalR WebSockets for real-time chat streaming
- IAsyncEnumerable for efficient streaming from providers

### Security Architecture
- Multi-layered security with DLP, sandbox, and threat detection
- Role-based access control (RBAC)
- Audit logging for compliance
- API key encryption and secure storage

### Deployment Architecture
- Docker containerization for consistent deployment
- docker-compose.yml for production
- docker-compose.dev.yml for development with hot reload
- Multi-arch support (amd64/arm64)
- Health checks and restart policies

## Configuration Management
- Layered configuration: appsettings.json → environment variables → user secrets
- Strongly-typed options pattern with validation
- Per-provider configuration classes
- Runtime configuration updates via API

## Testing Strategy
- xUnit for unit testing
- FluentAssertions for readable assertions
- Moq for mocking dependencies
- AutoFixture for test data generation
- Integration tests for infrastructure
- E2E tests for critical workflows

## File Organization Rules
- One type per file (enforced)
- Namespace matches folder structure
- Interfaces in Domain only
- Implementations in appropriate layers
- No circular dependencies between layers

## Technology Stack
- Backend: .NET 8, ASP.NET Core, SignalR, Entity Framework Core
- Frontend: Angular 20, TypeScript 5, Angular Material, RxJS
- Infrastructure: Docker, LibGit2Sharp
- Testing: xUnit, Moq, FluentAssertions, AutoFixture
- LLM Providers: OpenAI, Anthropic, Ollama

## Current State
The application is fully functional with:
- Complete web UI with chat interface
- Multiple LLM provider support
- Real-time streaming responses
- File system operations
- Git integration
- Comprehensive security features
- Docker deployment ready
- Configuration management system

## Development Notes
- Angular app builds to wwwroot directory
- Backend serves static files and API
- SignalR hub at /chat-hub endpoint
- CORS configured for development
- User secrets for local API keys
- Environment variables for Docker deployment