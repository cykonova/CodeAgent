# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CodeAgent is a .NET 8 web-based coding assistant application that integrates with multiple LLM providers through a modern Angular 20 frontend and ASP.NET Core Web API backend. The project follows Clean Architecture principles with Domain-Driven Design, separating concerns across multiple layers.

## Architecture

The solution uses Clean Architecture with the following layers:
- **CodeAgent.Domain**: Core domain models, interfaces, and business logic (no external dependencies)
  - Configuration models and provider settings
  - Security models (Role, SecurityPolicy, AuditEntry, DLP, Sandbox, Threat Detection)
  - Team collaboration models
  - MCP tool definitions and context
  - Chat models and session management
- **CodeAgent.Core**: Application services and use cases
  - Chat orchestration and context management
  - Provider management and factory patterns
  - Performance monitoring and telemetry
  - Plugin system and internal tools
- **CodeAgent.Infrastructure**: External service implementations
  - File system operations with change tracking
  - Git integration using LibGit2Sharp
  - Security services (DLP, Sandbox, Threat Detection)
  - Caching and session management
  - Audit logging and configuration persistence
- **CodeAgent.Providers**: LLM provider implementations
  - OpenAI provider with GPT model support
  - Claude provider with Anthropic API integration
  - Ollama provider for local model hosting
  - Docker provider for containerized models
- **CodeAgent.MCP**: Model Context Protocol implementation
  - MCP client for tool integration
  - Docker-based MCP provider support
- **CodeAgent.Web**: Modern web application
  - ASP.NET Core 8 Web API with SignalR real-time communication
  - Angular 20 frontend with Material Design components
  - Responsive chat interface with streaming support
- **CodeAgent.CLI**: Legacy console application (deprecated)

### Web Application Architecture

#### Backend (ASP.NET Core 8)
- **Controllers**:
  - `ChatController`: Chat message handling with streaming support via SignalR
  - `ConfigurationController`: Provider configuration and settings management
  - `FileController`: File system operations and browsing
  - `ModelsController`: LLM model discovery and management
  - `SecurityController`: Security policy and permission management
  - `HealthController`: Application health checks
- **SignalR Hubs**:
  - `ChatHub`: Real-time bidirectional communication for chat streaming
- **Services**: Dependency injection of domain and infrastructure services
- **CORS Configuration**: Support for cross-origin requests from Angular frontend

#### Frontend (Angular 20)
- **Components**:
  - Chat component with message streaming and markdown rendering
  - Configuration panel for provider settings
  - File browser with tree view navigation
  - Layout components (header, sidebar, main content)
  - About/help documentation
- **Services**:
  - `ChatService`: WebSocket communication via SignalR
  - `ConfigurationService`: Provider and model management
  - `FileService`: File system operations
- **Material Design**: Comprehensive UI component library
- **Responsive Design**: Mobile-friendly interface

## Development Commands

### Local Development (Native)
```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run the web application locally
dotnet run --project src/CodeAgent.Web

# Angular development server (if running separately)
cd src/CodeAgent.Web/client
npm install
npm run start
```

### Docker Development
```bash
# Development mode (with hot reload)
docker-compose -f docker-compose.dev.yml up --build

# Production mode
docker-compose -f docker-compose.yml up --build -d

# Stop containers
docker-compose down

# View logs
docker-compose logs -f
```

### Publishing and Deployment

#### Web Application Publishing
```bash
# Publish for production (platform-specific)
dotnet publish src/CodeAgent.CLI -c Release -r win-x64 --self-contained
dotnet publish src/CodeAgent.CLI -c Release -r linux-x64 --self-contained
dotnet publish src/CodeAgent.CLI -c Release -r osx-arm64 --self-contained
```

## Key Implementation Guidelines

### Project Structure Requirements
- **One type per file**: Each class, interface, enum, or record in its own file
- **Namespace hierarchy**: Must match folder structure exactly
- **Interface placement**: All interfaces in Domain project only
- **Implementation placement**: Concrete classes in appropriate layer (Core, Infrastructure, Providers)
- **Dependency injection**: Constructor injection for all dependencies
- **No circular dependencies**: Strict layer boundaries (Domain → Core → Infrastructure/Providers)

### LLM Provider Implementation
When implementing new LLM providers:
1. Create provider-specific project in CodeAgent.Providers namespace
2. Implement ILLMProvider interface from Domain project
3. Handle streaming responses with IAsyncEnumerable
4. Include proper error handling and retry logic
5. Support provider-specific configuration through options pattern

### File Operations
- Always use IFileSystemService abstraction for file operations
- Implement change tracking and rollback capability
- Show file diffs before applying changes
- Request user confirmation for destructive operations

### Git Integration
- Use LibGit2Sharp for Git operations
- Detect repository boundaries and respect .gitignore
- Track changes in Git-aware manner
- Support staging and commit operations

### Configuration Management
- **Configuration Sources**:
  - appsettings.json for default settings
  - appsettings.{Environment}.json for environment-specific settings
  - User Secrets for local development API keys
  - Environment variables for Docker/production deployment
  - Command-line arguments for overrides
- **Provider Configuration**:
  - Strongly-typed options pattern with IOptions<T>
  - Per-provider settings classes (OpenAIOptions, ClaudeOptions, etc.)
  - Support for multiple provider profiles
  - Runtime configuration updates via API
- **Security**:
  - Never log API keys or sensitive data
  - Encrypt sensitive configuration in transit
  - Use Azure Key Vault or similar in production

### Testing Requirements
- Write unit tests for all domain logic
- Use xUnit, FluentAssertions, Moq, and AutoFixture
- Mock external dependencies in unit tests
- Create integration tests for infrastructure components
- Test file system operations with temporary directories
- Ensure cross-platform compatibility in tests

### Safety and Security
- Never log or expose API keys or sensitive configuration
- Implement confirmation prompts for file modifications
- Support dry-run mode for testing changes
- Provide rollback capability for applied changes
- Validate and sanitize all file paths

## Project Status

**Architecture**: Production-Ready Clean Architecture Implementation
- Full separation of concerns with DDD principles
- Comprehensive security layer with DLP, sandbox, and threat detection
- Team collaboration and multi-tenancy support
- Plugin architecture for extensibility
- Performance monitoring and telemetry

**Technology Stack**:
- **Backend**: .NET 8, ASP.NET Core, SignalR, Entity Framework Core
- **Frontend**: Angular 20, Angular Material, RxJS, TypeScript 5
- **Providers**: OpenAI GPT-4, Claude 3, Ollama, Docker-based models
- **Infrastructure**: Docker, Docker Compose, nginx proxy
- **Testing**: xUnit, FluentAssertions, Moq, AutoFixture

**Deployment Options**:
1. **Docker Compose**: Production and development configurations
2. **Kubernetes**: Helm charts for orchestration (planned)
3. **Cloud Native**: Azure Container Apps / AWS ECS ready
4. **Standalone**: Direct .NET hosting with Kestrel

**Security Features**:
- Role-based access control (RBAC)
- Data Loss Prevention (DLP) scanning
- Sandboxed code execution
- Threat detection and audit logging
- API key encryption and secure storage
- CORS and CSP policies

## Design Patterns and Principles

### Architectural Patterns
- **Clean Architecture**: Strict layer separation with dependency inversion
- **Domain-Driven Design**: Rich domain models with business logic
- **CQRS**: Command/Query separation for chat operations
- **Repository Pattern**: Abstraction over data persistence
- **Unit of Work**: Transaction management for complex operations

### Implementation Patterns
- **Dependency Injection**: Constructor injection with interface segregation
- **Factory Pattern**: Provider creation and model instantiation
- **Strategy Pattern**: Multiple LLM provider implementations
- **Observer Pattern**: SignalR hub for real-time updates
- **Decorator Pattern**: Caching and retry logic wrappers
- **Chain of Responsibility**: Security policy evaluation

### Coding Patterns
- **Async/Await**: All I/O operations are asynchronous
- **IAsyncEnumerable**: Streaming responses from LLM providers
- **Result<T> Pattern**: Explicit error handling without exceptions
- **Options Pattern**: Strongly-typed configuration with validation
- **Cancellation Tokens**: Proper cancellation support throughout
- **Dispose Pattern**: Resource cleanup with IDisposable/IAsyncDisposable

## Cross-Platform Considerations
- **File Paths**: Always use Path.Combine() and Path.DirectorySeparatorChar
- **Line Endings**: Environment.NewLine for text files, preserve original for code
- **Case Sensitivity**: Assume case-sensitive file systems (Linux/macOS)
- **Permissions**: Handle Unix permissions and Windows ACLs appropriately
- **Docker**: Multi-arch images for linux/amd64 and linux/arm64
- **Time Zones**: Use UTC internally, convert for display
- **Encoding**: UTF-8 for all text operations

## Development Workflow

### Git Workflow
- Feature branches from main
- Conventional commits (feat:, fix:, docs:, refactor:, test:)
- Pull requests with code review
- Squash and merge to maintain clean history
- Semantic versioning for releases

### Testing Strategy
- Unit tests for all business logic (Domain/Core)
- Integration tests for infrastructure components
- E2E tests for critical user workflows
- Performance tests for LLM provider operations
- Security tests for authentication/authorization

### Code Quality
- EditorConfig for consistent formatting
- Roslyn analyzers for code quality
- SonarQube for static analysis
- Code coverage minimum 80%
- Documentation for public APIs

### CI/CD Pipeline
- GitHub Actions for CI/CD
- Automated testing on PR
- Docker image building and pushing
- Deployment to staging/production
- Rollback capability