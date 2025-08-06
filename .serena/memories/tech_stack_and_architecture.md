# Tech Stack and Architecture

## Tech Stack
- **.NET 8.0 SDK**: Core framework
- **Spectre.Console**: Rich terminal UI and CLI framework
- **System.CommandLine**: Command-line parsing
- **Microsoft.Extensions.***: Dependency injection, configuration, logging, hosting
- **Markdig**: Markdown rendering
- **xUnit**: Testing framework
- **FluentAssertions**: Assertion library for tests
- **Moq**: Mocking framework (implied by project structure)

## Architecture
The solution follows Clean Architecture principles with layered separation:

### Projects Structure
- **CodeAgent.Domain**: Core business logic, entities, and abstractions (no external dependencies)
- **CodeAgent.Core**: Application services and use cases
- **CodeAgent.Infrastructure**: External service implementations (file system, configuration)
- **CodeAgent.Providers**: LLM provider implementations (OpenAI, Claude, Ollama)
- **CodeAgent.MCP**: Model Context Protocol client implementation
- **CodeAgent.CLI**: Console application entry point using Spectre.Console

### Key Patterns
- **Dependency Injection**: Constructor injection with interfaces defined in Domain
- **Async/Await**: Used throughout for I/O operations and API calls
- **Command Pattern**: CLI commands follow command pattern with separate handler classes
- **Configuration**: Strongly-typed configuration with IOptions<T> pattern
- **Interface Segregation**: Clear separation between domain interfaces and implementations