# Code Style and Conventions

## General Conventions
- **One type per file**: Each class, interface, enum, or record gets its own file
- **Namespace hierarchy**: Follows folder structure exactly
- **Nullable enabled**: All projects use `<Nullable>enable</Nullable>`
- **Implicit usings**: Enabled across all projects
- **Target Framework**: .NET 8.0 for all projects

## Naming Conventions
- **Interfaces**: Prefixed with `I` (e.g., `IFileSystemService`)
- **Services**: Suffix with `Service` (e.g., `FileSystemService`)
- **Commands**: Suffix with `Command` (e.g., `ScanCommand`)
- **Tests**: Suffix with `Tests` (e.g., `FileSystemServiceTests`)

## Project Structure
- **Domain**: Contains interfaces in `Interfaces/` folder, models in `Models/` folder
- **Infrastructure**: Implementations in `Services/` folder
- **CLI**: Commands in `Commands/` folder, shell components in `Shell/` folder
- **Tests**: Mirror the source project structure

## Testing Conventions
- **xUnit** for test framework
- **FluentAssertions** for assertions (`.Should()` syntax)
- **Arrange/Act/Assert** pattern in test methods
- **IDisposable** pattern for test cleanup with temporary directories
- **Cross-platform** path handling using `Path.Combine()` and `Path.DirectorySeparatorChar`

## Async/Await Usage
- All I/O operations are async
- Use `CancellationToken` parameters with default values
- Return `Task<T>` for async methods

## Dependency Injection
- Constructor injection for all dependencies
- Services registered in host builder
- Interfaces defined in Domain, implementations in appropriate layer