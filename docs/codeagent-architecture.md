# CodeAgent System Architecture & Project Structure

## Solution Structure

```
CodeAgent.sln
├── src/
│   ├── CodeAgent.CLI/                  # Entry point & Spectre.Console UI
│   ├── CodeAgent.Core/                 # Application services & orchestration
│   ├── CodeAgent.Domain/               # Business logic & abstractions
│   ├── CodeAgent.Infrastructure/       # External service implementations
│   ├── CodeAgent.Providers/            # LLM provider implementations
│   └── CodeAgent.MCP/                  # Model Context Protocol client
├── tests/
│   ├── CodeAgent.CLI.Tests/            # CLI command & UI tests
│   ├── CodeAgent.Core.Tests/           # Application service tests
│   ├── CodeAgent.Domain.Tests/         # Domain logic & model tests
│   ├── CodeAgent.Infrastructure.Tests/ # Infrastructure integration tests
│   ├── CodeAgent.Providers.Tests/      # LLM provider tests
│   ├── CodeAgent.MCP.Tests/            # MCP protocol tests
│   └── CodeAgent.Integration.Tests/    # End-to-end integration tests
├── docs/
│   ├── architecture/
│   ├── user-guide/
│   └── development/
├── samples/
│   ├── configurations/
│   └── workflows/
└── scripts/
    ├── build.ps1
    ├── test.ps1
    └── publish.ps1
```

## Project Details & Dependencies

### **CodeAgent.CLI** (Console Application)
**Purpose**: Entry point, command parsing, and user interface
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Spectre.Console" Version="0.48.0" />
    <PackageReference Include="Spectre.Console.Cli" Version="0.48.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="8.0.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../CodeAgent.Core/CodeAgent.Core.csproj" />
    <ProjectReference Include="../CodeAgent.Infrastructure/CodeAgent.Infrastructure.csproj" />
    <ProjectReference Include="../CodeAgent.Providers/CodeAgent.Providers.csproj" />
    <ProjectReference Include="../CodeAgent.MCP/CodeAgent.MCP.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Core** (Class Library)
**Purpose**: Application services, session management, workflow orchestration
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../CodeAgent.Domain/CodeAgent.Domain.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Domain** (Class Library)
**Purpose**: Core business models, interfaces, and domain logic
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Infrastructure** (Class Library)
**Purpose**: File system, Git operations, configuration, external service implementations
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="LibGit2Sharp" Version="0.29.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.FileProviders.Abstractions" Version="8.0.0" />
    <PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../CodeAgent.Domain/CodeAgent.Domain.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Providers** (Class Library)
**Purpose**: LLM provider implementations (OpenAI, Claude, Ollama, etc.)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../CodeAgent.Domain/CodeAgent.Domain.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.MCP** (Class Library)
**Purpose**: Model Context Protocol client implementation
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
    <PackageReference Include="System.Net.WebSockets.Client" Version="4.3.2" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../CodeAgent.Domain/CodeAgent.Domain.csproj" />
  </ItemGroup>
</Project>
```

## Test Projects

### **CodeAgent.CLI.Tests** (Test Project)
**Purpose**: Command handlers, UI components, integration scenarios
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Testing" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../src/CodeAgent.CLI/CodeAgent.CLI.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Core.Tests** (Test Project)
**Purpose**: Application services, session management, workflow tests
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Testing" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../src/CodeAgent.Core/CodeAgent.Core.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Domain.Tests** (Test Project)
**Purpose**: Domain models, business logic, validation rules
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="AutoFixture" Version="4.18.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../src/CodeAgent.Domain/CodeAgent.Domain.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Infrastructure.Tests** (Test Project)
**Purpose**: File operations, Git integration, configuration management
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="Testcontainers" Version="3.6.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Testing" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../src/CodeAgent.Infrastructure/CodeAgent.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Providers.Tests** (Test Project)
**Purpose**: LLM provider implementations, API integrations
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="WireMock.Net" Version="1.5.42" />
    <PackageReference Include="Microsoft.Extensions.Http.Testing" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../src/CodeAgent.Providers/CodeAgent.Providers.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.MCP.Tests** (Test Project)
**Purpose**: MCP protocol implementation, server communication
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="WireMock.Net" Version="1.5.42" />
    <PackageReference Include="Microsoft.Extensions.Logging.Testing" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../src/CodeAgent.MCP/CodeAgent.MCP.csproj" />
  </ItemGroup>
</Project>
```

### **CodeAgent.Integration.Tests** (Test Project)
**Purpose**: End-to-end scenarios, full workflow testing
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Testcontainers" Version="3.6.0" />
    <PackageReference Include="Docker.DotNet" Version="3.125.15" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Testing" Version="8.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../src/CodeAgent.CLI/CodeAgent.CLI.csproj" />
    <ProjectReference Include="../../src/CodeAgent.Core/CodeAgent.Core.csproj" />
    <ProjectReference Include="../../src/CodeAgent.Infrastructure/CodeAgent.Infrastructure.csproj" />
    <ProjectReference Include="../../src/CodeAgent.Providers/CodeAgent.Providers.csproj" />
    <ProjectReference Include="../../src/CodeAgent.MCP/CodeAgent.MCP.csproj" />
  </ItemGroup>
</Project>
```

## Key Architectural Components

### **Domain Layer Interfaces**
```csharp
// Core abstractions in CodeAgent.Domain
public interface ILLMProvider
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
    Task<IAsyncEnumerable<string>> StreamChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
    ProviderCapabilities Capabilities { get; }
    string ProviderId { get; }
}

public interface IMCPClient
{
    Task<MCPConnection> ConnectAsync(string serverUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MCPTool>> GetToolsAsync(string serverId, CancellationToken cancellationToken = default);
    Task<MCPResponse> ExecuteToolAsync(string serverId, string toolName, object parameters, CancellationToken cancellationToken = default);
}

public interface ICodeAnalyzer
{
    Task<ProjectStructure> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<FileAnalysis> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeIssue>> DetectIssuesAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IChangeManager
{
    Task<FileDiff> GenerateDiffAsync(string filePath, string newContent, CancellationToken cancellationToken = default);
    Task<ApplyResult> ApplyChangesAsync(IReadOnlyList<FileDiff> diffs, CancellationToken cancellationToken = default);
    Task RollbackAsync(string changeId, CancellationToken cancellationToken = default);
}
```

### **Application Layer Services**
```csharp
// Core services in CodeAgent.Core
public interface ISessionManager
{
    Task<Session> CreateSessionAsync(SessionOptions options, CancellationToken cancellationToken = default);
    Task<Session> GetActiveSessionAsync(CancellationToken cancellationToken = default);
    Task SaveSessionAsync(Session session, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Session>> GetSessionHistoryAsync(CancellationToken cancellationToken = default);
}

public interface IWorkflowEngine
{
    Task<WorkflowResult> ExecuteAsync(string workflowName, WorkflowContext context, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> GetAvailableWorkflowsAsync(CancellationToken cancellationToken = default);
    Task RegisterWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);
}

public interface IContextManager
{
    Task<ProjectContext> BuildContextAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRelevantFilesAsync(string query, int maxFiles = 10, CancellationToken cancellationToken = default);
    Task UpdateContextAsync(ProjectContext context, IReadOnlyList<string> modifiedFiles, CancellationToken cancellationToken = default);
}
```

## Testing Strategy

### **Unit Tests** (Fast, Isolated)
- **Domain models** with business logic validation
- **Service classes** with mocked dependencies
- **Provider implementations** with fake HTTP responses
- **Algorithm implementations** with known inputs/outputs

### **Integration Tests** (Medium Speed)
- **Database interactions** with test containers
- **File system operations** with temporary directories
- **Git operations** with test repositories
- **Provider integrations** with mock servers

### **End-to-End Tests** (Slower, Full Stack)
- **Complete CLI workflows** with real file systems
- **Multi-provider scenarios** with multiple mock services
- **MCP integration** with test MCP servers
- **Error handling** and recovery scenarios

### **Test Utilities & Fixtures**
```csharp
// Shared test infrastructure
public class TestProjectBuilder
{
    public TestProjectBuilder WithFile(string path, string content);
    public TestProjectBuilder WithGitRepository();
    public TestProject Build();
}

public class MockLLMProvider : ILLMProvider
{
    public void SetupResponse(string query, string response);
    public void SetupStreamingResponse(string query, IEnumerable<string> chunks);
}

public class TestFileSystem : IFileSystem
{
    // In-memory file system for fast testing
}
```

## Build & Development Scripts

### **build.ps1**
```powershell
# Build all projects and run tests
dotnet build --configuration Release
dotnet test --no-build --verbosity normal
```

### **test.ps1**
```powershell
# Run comprehensive test suite
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./TestResults/html" -reporttypes:Html
```

### **publish.ps1**
```powershell
# Publish platform-specific binaries
dotnet publish src/CodeAgent.CLI -c Release -r win-x64 --self-contained
dotnet publish src/CodeAgent.CLI -c Release -r linux-x64 --self-contained
dotnet publish src/CodeAgent.CLI -c Release -r osx-x64 --self-contained
```

This architecture provides a solid foundation for building a robust, testable, and extensible coding agent with comprehensive test coverage across all layers.