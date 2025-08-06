# Suggested Development Commands

## Building and Testing
```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Restore packages
dotnet restore
```

## Running the Application
```bash
# Run the CLI application directly
dotnet run --project src/CodeAgent.CLI

# Run as global tool (if installed)
codeagent

# Interactive mode
codeagent
# Or
dotnet run --project src/CodeAgent.CLI

# Command mode examples
codeagent scan
codeagent setup
codeagent --help

# Piped input
echo "Explain this code" | codeagent
echo "/scan" | codeagent
```

## Packaging and Publishing
```bash
# Create NuGet package
dotnet pack src/CodeAgent.CLI/CodeAgent.CLI.csproj -c Release

# Publish for specific platform
dotnet publish src/CodeAgent.CLI -c Release -r win-x64 --self-contained
dotnet publish src/CodeAgent.CLI -c Release -r linux-x64 --self-contained
dotnet publish src/CodeAgent.CLI -c Release -r osx-arm64 --self-contained

# Install as global tool locally
dotnet tool install --global --add-source ./src/CodeAgent.CLI/nupkg Cykonova.CodeAgent
```

## Development Workflow
```bash
# Clean solution
dotnet clean

# Watch and rebuild on changes
dotnet watch --project src/CodeAgent.CLI run

# Run specific test project
dotnet test tests/CodeAgent.Infrastructure.Tests

# Run specific test method
dotnet test --filter "MethodName"
```

## macOS/Darwin Specific Commands
```bash
# Standard Unix commands available:
ls, cd, grep, find, git

# File operations
cat filename.cs
head -20 filename.cs
tail -20 filename.cs

# Search operations
find . -name "*.cs" -type f
grep -r "FileSystem" src/
```