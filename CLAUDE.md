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
- **CodeAgent.CLI**: Console application entry point using Spectre.Console

## Development Commands

Once the project is implemented, use these commands:

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run the application
dotnet run --project src/CodeAgent.CLI

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

**Current Phase**: Planning/Documentation
- Requirements documented in docs/coding-agent-requirements.md
- Architecture specified in docs/codeagent-architecture.md
- No source code implemented yet

**Next Implementation Steps**:
1. Create solution and project structure
2. Implement domain models and interfaces
3. Build CLI with Spectre.Console
4. Add first LLM provider (start with OpenAI or Claude)
5. Implement file system operations with safety features
6. Add Git integration
7. Create comprehensive test suite

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