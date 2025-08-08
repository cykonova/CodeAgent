# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CodeAgent is a .NET 8 cross-platform command-line coding assistant application that integrates with multiple LLM providers. The project follows Clean Architecture principles with separated concerns across multiple projects.

## Architecture

The solution uses a layered architecture:
- **CodeAgent.Domain**: Core business logic, entities, and abstractions (no external dependencies)
- **CodeAgent.Core**: Application services and use cases
- **CodeAgent.Infrastructure**: External service implementations (file system, Git, database)
- **CodeAgent.Providers**: LLM provider implementations (OpenAI, Claude, Ollama, LMStudio)
- **CodeAgent.MCP**: Model Context Protocol client implementation
- **CodeAgent.Web**: ASP.NET Core Web API backend with Angular 18 frontend
- **CodeAgent.CLI**: Console application entry point using Spectre.Console (legacy)

### Web Application Architecture
The web application consists of:
- **Backend**: ASP.NET Core 8 Web API with SignalR for real-time communication
- **Frontend**: Angular 18 with Material Design components
- **Communication**: REST API endpoints + SignalR hubs for streaming responses
- **Key Controllers**: 
  - `ChatController`: Handles chat messages and streaming
  - `ConfigurationController`: Manages provider settings and configuration
  - `FilesController`: File system operations
  - `ModelsController`: LLM model management

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

### Legacy CLI Publishing (Original Design)
```bash
# Publish for production (platform-specific)
dotnet publish src/CodeAgent.CLI -c Release -r win-x64 --self-contained
dotnet publish src/CodeAgent.CLI -c Release -r linux-x64 --self-contained
dotnet publish src/CodeAgent.CLI -c Release -r osx-arm64 --self-contained
```

## Key Implementation Guidelines

### Project Structure Requirements
- One type per file (classes, interfaces, enums, records)
- Follow namespace hierarchy matching folder structure
- Place interfaces in Domain project, implementations in appropriate layer
- Use dependency injection for all service dependencies

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
- Use Microsoft.Extensions.Configuration
- Store sensitive data (API keys) in User Secrets during development
- Support environment variables for production
- Implement provider profiles for multiple configurations

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

**Current Phase**: Full Implementation Complete
- Web-based application with Angular 18 frontend and .NET 8 Web API backend
- Docker containerization for both development and production deployments
- Multiple LLM provider integrations (OpenAI, Claude, Ollama, LM Studio)
- SignalR for real-time communication
- Provider management and configuration system
- Chat interface with tool calling and streaming support
- File browser and configuration panels

**Implemented Components**:
1. ✅ Solution and project structure (Clean Architecture)
2. ✅ Domain models and interfaces
3. ✅ Web interface with Angular Material Design
4. ✅ All major LLM providers (OpenAI, Claude, Ollama, LM Studio)
5. ✅ File system operations and Git integration
6. ✅ Comprehensive configuration management
7. ✅ Docker deployment infrastructure

**Recent Fixes Applied**:
- Fixed backend/frontend data contract mismatch in ConfigurationController
- Resolved provider UI issues (provider/model selection dropdowns)
- Updated Angular components for proper Material Design integration
- Fixed vertical alignment in provider configuration lists

## Important Patterns

### Dependency Injection
All services use constructor injection with interfaces defined in Domain project.

### Async/Await
Use async operations throughout for I/O operations and API calls.

### Result Pattern
Consider using Result<T> pattern for operations that can fail instead of exceptions.

### Command Pattern
CLI commands follow command pattern with separate handler classes.

### Configuration
Use strongly-typed configuration with IOptions<T> pattern.

## Cross-Platform Considerations
- Use Path.Combine() for file paths
- Handle line endings appropriately (Environment.NewLine)
- Test on Windows, Linux, and macOS
- Use platform-agnostic file system operations
- Support both forward and backward slashes in paths

## Project Management Notes

- This is your project, I'm just the project planner. Commit and push as you see fit.
- Rebuild the docker container after tasks are complete so I may review your progress