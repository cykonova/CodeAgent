# Task Completion Checklist

When completing any development task in CodeAgent, follow these steps:

## Code Quality Checks
1. **Build Verification**
   ```bash
   dotnet build
   ```
   - Ensure no compilation errors
   - Fix any warnings if possible

2. **Run Tests**
   ```bash
   dotnet test
   ```
   - All tests must pass
   - Add new tests for new functionality
   - Update existing tests if behavior changed

3. **Code Style Compliance**
   - Follow one-type-per-file rule
   - Use proper naming conventions
   - Ensure nullable reference types are handled
   - Add XML documentation for public APIs

## Design Pattern Adherence
- **SOLID Principles**: Ensure code follows SOLID design principles
- **Clean Architecture**: Maintain proper layer separation
- **Dependency Injection**: Use constructor injection with interfaces
- **Async/Await**: Use async patterns for I/O operations

## Cross-Platform Considerations
- Use `Path.Combine()` for file paths
- Handle line endings with `Environment.NewLine`
- Test path separator handling with `Path.DirectorySeparatorChar`
- Ensure compatibility across Windows, Linux, and macOS

## Git Workflow
After code completion:
1. **Stage Changes**
   ```bash
   git add .
   ```

2. **Commit Changes**
   ```bash
   git commit -m "descriptive commit message"
   ```
   - Use clear, descriptive commit messages
   - Follow conventional commit format if established

## Safety and Validation
- **File Operations**: Ensure safe file handling with proper error handling
- **Configuration**: Never commit sensitive data (API keys, secrets)
- **Testing**: Test with temporary directories for file operations
- **Rollback**: Ensure operations can be undone if needed