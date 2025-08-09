# Code Agent - Docker Sandbox Architecture & MCP Integration

## Overview

The Code Agent system provides complete project isolation through Docker containerization, with each project running in its own sandboxed environment. This architecture supports Docker Desktop MCP (Beta), Docker LLM providers, and ensures secure execution of code while maintaining full configurability through CLI and web interfaces.

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│          Code Agent Host System             │
├─────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌───────────┐ │
│  │ Web UI   │  │   CLI    │  │    API    │ │
│  └──────────┘  └──────────┘  └───────────┘ │
│         │            │             │        │
│         └────────────┼─────────────┘        │
│                      ▼                      │
│          ┌──────────────────┐               │
│          │ Orchestrator     │               │
│          │ & Router         │               │
│          └──────────────────┘               │
│                      │                      │
├──────────────────────┼──────────────────────┤
│                      ▼                      │
│  ┌──────────────────────────────────────┐  │
│  │      Docker Sandbox Manager          │  │
│  └──────────────────────────────────────┘  │
│         │            │            │         │
│         ▼            ▼            ▼         │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│  │ Project  │ │ Project  │ │ Project  │  │
│  │ Sandbox  │ │ Sandbox  │ │ Sandbox  │  │
│  │    A     │ │    B     │ │    C     │  │
│  └──────────┘ └──────────┘ └──────────┘  │
└─────────────────────────────────────────────┘
```

## Docker Desktop MCP Integration

### MCP Server Configuration
```yaml
# docker-mcp-config.yaml
mcp_servers:
  docker_desktop:
    type: docker_desktop_mcp
    version: beta
    endpoint: unix:///var/run/docker.sock
    capabilities:
      - container_management
      - image_management
      - volume_management
      - network_management
      - compose_orchestration
    
  docker_llm:
    type: docker_llm
    version: beta
    models_path: /var/lib/docker-llm/models
    capabilities:
      - model_containerization
      - gpu_passthrough
      - resource_isolation
```

### MCP Tools Available
```typescript
interface DockerMCPTools {
  // Container Management
  'docker.container.create': (config: ContainerConfig) => Container;
  'docker.container.start': (id: string) => void;
  'docker.container.stop': (id: string) => void;
  'docker.container.exec': (id: string, command: string[]) => ExecResult;
  'docker.container.logs': (id: string, options?: LogOptions) => string;
  'docker.container.remove': (id: string) => void;
  
  // Image Management
  'docker.image.build': (dockerfile: string, context: string) => Image;
  'docker.image.pull': (name: string) => Image;
  'docker.image.push': (name: string) => void;
  
  // Compose Operations
  'docker.compose.up': (file: string, options?: ComposeOptions) => void;
  'docker.compose.down': (file: string) => void;
  
  // File Operations in Container
  'docker.file.read': (container: string, path: string) => string;
  'docker.file.write': (container: string, path: string, content: string) => void;
  'docker.file.list': (container: string, path: string) => FileInfo[];
}
```

## Docker LLM Provider

### Configuration
```yaml
# docker-llm-provider.yaml
providers:
  docker_llm:
    type: docker_llm
    endpoint: http://docker-llm-gateway:8090
    models:
      - id: llama3-container
        image: docker-llm/llama3:latest
        resources:
          memory: 8G
          cpus: 4
          gpus: 1
      - id: codellama-container
        image: docker-llm/codellama:34b
        resources:
          memory: 16G
          cpus: 8
          gpus: 2
```

### Docker LLM Deployment
```dockerfile
# Dockerfile.llm
FROM nvidia/cuda:12.2.0-runtime-ubuntu22.04

# Install model server
RUN pip install docker-llm-server

# Copy model files
COPY models/ /models/

# Configure MCP endpoint
ENV MCP_ENABLED=true
ENV MCP_PORT=9090

# Start LLM server with MCP support
CMD ["docker-llm-server", "--mcp", "--model-path", "/models"]
```

## Project Sandbox Architecture

### Sandbox Container Specification
```dockerfile
# Dockerfile.sandbox
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install development tools
RUN apt-get update && apt-get install -y \
    git \
    nodejs \
    npm \
    python3 \
    python3-pip \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Install MCP agent executor
COPY agent-executor /usr/local/bin/
RUN chmod +x /usr/local/bin/agent-executor

# Create project directory
WORKDIR /workspace

# Install MCP tools
RUN npm install -g @modelcontextprotocol/cli

# Configure MCP agent
ENV MCP_MODE=sandbox
ENV MCP_PROJECT_ID=${PROJECT_ID}
ENV MCP_SECURITY_LEVEL=development

# Start agent executor
ENTRYPOINT ["agent-executor"]
CMD ["--listen", "0.0.0.0:8500"]
```

### Sandbox Creation & Management
```csharp
public class SandboxManager
{
    private readonly IDockerClient _docker;
    private readonly Dictionary<string, SandboxContainer> _sandboxes;
    
    public async Task<SandboxContainer> CreateProjectSandbox(Project project)
    {
        var config = new CreateContainerParameters
        {
            Image = "codeagent/sandbox:latest",
            Name = $"codeagent-{project.Id}",
            Env = new[]
            {
                $"PROJECT_ID={project.Id}",
                $"PROJECT_NAME={project.Name}",
                $"SECURITY_LEVEL={project.SecurityLevel}",
                $"MCP_ENABLED=true"
            },
            HostConfig = new HostConfig
            {
                // Resource limits
                Memory = project.MemoryLimit ?? 4_294_967_296, // 4GB default
                CpuShares = project.CpuShares ?? 1024,
                
                // Volume mounts
                Mounts = new[]
                {
                    new Mount
                    {
                        Type = "bind",
                        Source = project.SourcePath,
                        Target = "/workspace/src",
                        ReadOnly = false
                    },
                    new Mount
                    {
                        Type = "volume",
                        Source = $"{project.Id}-data",
                        Target = "/workspace/data"
                    }
                },
                
                // Network configuration
                NetworkMode = project.NetworkMode ?? "bridge",
                CapDrop = new[] { "ALL" },
                CapAdd = project.RequiredCapabilities ?? new[] { "CHOWN", "SETUID", "SETGID" },
                
                // Security options
                SecurityOpt = new[] { "no-new-privileges" },
                ReadonlyRootfs = project.ReadOnlyRootFs ?? false
            }
        };
        
        var container = await _docker.Containers.CreateContainerAsync(config);
        await _docker.Containers.StartContainerAsync(container.ID);
        
        var sandbox = new SandboxContainer
        {
            Id = container.ID,
            ProjectId = project.Id,
            Status = ContainerStatus.Running,
            CreatedAt = DateTime.UtcNow,
            AgentEndpoint = $"http://{container.ID}:8500"
        };
        
        _sandboxes[project.Id] = sandbox;
        return sandbox;
    }
    
    public async Task<MCPResponse> ExecuteInSandbox(string projectId, MCPMessage message)
    {
        var sandbox = _sandboxes[projectId];
        
        // Send MCP message to agent in container
        using var client = new HttpClient();
        var response = await client.PostAsJsonAsync(
            $"{sandbox.AgentEndpoint}/mcp/execute",
            message
        );
        
        return await response.Content.ReadFromJsonAsync<MCPResponse>();
    }
}
```

### MCP Agent Executor (Runs Inside Sandbox)
```csharp
// agent-executor/Program.cs
public class AgentExecutor
{
    private readonly MCPServer _mcpServer;
    private readonly ProjectContext _context;
    private readonly FileSystemWatcher _watcher;
    
    public async Task Start()
    {
        // Initialize MCP server
        _mcpServer = new MCPServer(8500);
        
        // Register MCP tools
        _mcpServer.RegisterTool("exec", ExecuteCommand);
        _mcpServer.RegisterTool("file.read", ReadFile);
        _mcpServer.RegisterTool("file.write", WriteFile);
        _mcpServer.RegisterTool("build", BuildProject);
        _mcpServer.RegisterTool("test", RunTests);
        _mcpServer.RegisterTool("git", GitOperation);
        
        // Start file system watcher
        _watcher = new FileSystemWatcher("/workspace/src");
        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
        
        // Start MCP server
        await _mcpServer.StartAsync();
    }
    
    private async Task<ToolResult> ExecuteCommand(ToolRequest request)
    {
        var command = request.Parameters["command"].ToString();
        var args = request.Parameters["args"]?.ToString() ?? "";
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = "/workspace",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return new ToolResult
        {
            Success = process.ExitCode == 0,
            Output = output,
            Error = error,
            ExitCode = process.ExitCode
        };
    }
}
```

## Configuration Management

### CLI Configuration Commands
```bash
# Global provider configuration
codeagent config provider add docker-llm \
  --type docker \
  --endpoint http://localhost:8090

# Configure Docker Desktop MCP
codeagent config mcp enable docker-desktop
codeagent config mcp set docker.socket /var/run/docker.sock

# Project sandbox configuration
codeagent project config sandbox \
  --project my-app \
  --memory 8G \
  --cpus 4 \
  --security-level development \
  --network bridge

# Plugin management
codeagent plugin install docker-mcp-tools
codeagent plugin config docker-mcp-tools \
  --enable-compose \
  --enable-swarm

# List sandbox containers
codeagent sandbox list
codeagent sandbox logs my-app --follow
codeagent sandbox exec my-app -- npm test
```

### Web UI Configuration Interface
```typescript
// ConfigurationManager.tsx
interface SandboxConfig {
  projectId: string;
  enabled: boolean;
  image: string;
  resources: {
    memory: string;
    cpus: number;
    gpus?: number;
  };
  security: {
    level: 'full-isolation' | 'development' | 'build' | 'production';
    networkMode: 'none' | 'bridge' | 'host';
    readOnlyRootFs: boolean;
    capabilities: string[];
  };
  volumes: Array<{
    source: string;
    target: string;
    readOnly: boolean;
  }>;
  environment: Record<string, string>;
}

const SandboxConfigPanel: React.FC = () => {
  const [config, setConfig] = useState<SandboxConfig>();
  
  return (
    <div className="sandbox-config">
      <h2>Project Sandbox Configuration</h2>
      
      <Toggle 
        label="Enable Sandbox"
        checked={config.enabled}
        onChange={(enabled) => updateConfig({enabled})}
      />
      
      <Select
        label="Security Level"
        value={config.security.level}
        options={[
          {value: 'full-isolation', label: 'Full Isolation (No Network)'},
          {value: 'development', label: 'Development (Local Network)'},
          {value: 'build', label: 'Build (Package Access)'},
          {value: 'production', label: 'Production (Full Access)'}
        ]}
        onChange={(level) => updateSecurityLevel(level)}
      />
      
      <ResourceConfig
        memory={config.resources.memory}
        cpus={config.resources.cpus}
        gpus={config.resources.gpus}
        onChange={updateResources}
      />
      
      <VolumeManager
        volumes={config.volumes}
        onAdd={addVolume}
        onRemove={removeVolume}
      />
      
      <EnvironmentVariables
        vars={config.environment}
        onChange={updateEnvironment}
      />
      
      <button onClick={applySandboxConfig}>
        Apply Configuration
      </button>
    </div>
  );
};
```

## Security Levels

### Full Isolation
```yaml
security:
  level: full-isolation
  network: none
  filesystem: read-only
  capabilities: []
  syscalls: minimal
  internet: blocked
  local_network: blocked
```

### Development Mode
```yaml
security:
  level: development
  network: bridge
  filesystem: read-write
  capabilities: [CHOWN, SETUID, SETGID]
  syscalls: standard
  internet: restricted
  local_network: allowed
```

### Build Mode
```yaml
security:
  level: build
  network: bridge
  filesystem: read-write
  capabilities: [CHOWN, SETUID, SETGID, NET_BIND_SERVICE]
  syscalls: standard
  internet: package_managers_only
  local_network: allowed
```

### Production Mode
```yaml
security:
  level: production
  network: custom
  filesystem: read-write
  capabilities: [ALL]
  syscalls: unrestricted
  internet: allowed
  local_network: allowed
  audit_logging: enabled
```

## Plugin Architecture

### Plugin Structure
```
plugins/
├── docker-mcp-tools/
│   ├── manifest.json
│   ├── index.js
│   └── mcp-tools.js
├── docker-llm-provider/
│   ├── manifest.json
│   ├── provider.cs
│   └── models/
└── sandbox-manager/
    ├── manifest.json
    └── manager.cs
```

### Plugin Manifest
```json
{
  "name": "docker-mcp-tools",
  "version": "1.0.0",
  "type": "mcp-provider",
  "author": "CodeAgent Team",
  "description": "Docker Desktop MCP integration tools",
  "main": "index.js",
  "requirements": {
    "docker": ">=24.0.0",
    "mcp": ">=1.0.0-beta"
  },
  "configuration": {
    "docker_socket": {
      "type": "string",
      "default": "/var/run/docker.sock",
      "description": "Docker socket path"
    },
    "enable_compose": {
      "type": "boolean",
      "default": true,
      "description": "Enable Docker Compose support"
    }
  },
  "permissions": [
    "docker.access",
    "filesystem.read",
    "filesystem.write",
    "network.local"
  ]
}
```

### Plugin Loading
```csharp
public class PluginManager
{
    private readonly Dictionary<string, IPlugin> _plugins = new();
    
    public async Task LoadPlugin(string path)
    {
        var manifest = await LoadManifest(path);
        
        // Validate permissions
        if (!await ValidatePermissions(manifest.Permissions))
            throw new SecurityException("Plugin requires unauthorized permissions");
        
        // Load plugin based on type
        IPlugin plugin = manifest.Type switch
        {
            "mcp-provider" => new MCPProviderPlugin(path),
            "llm-provider" => new LLMProviderPlugin(path),
            "tool" => new ToolPlugin(path),
            _ => throw new NotSupportedException($"Plugin type {manifest.Type} not supported")
        };
        
        await plugin.Initialize(manifest.Configuration);
        _plugins[manifest.Name] = plugin;
        
        // Register with appropriate service
        if (plugin is IMCPProvider mcpProvider)
            _mcpRegistry.Register(mcpProvider);
    }
}
```

## Sandbox Lifecycle Management

### Automatic Lifecycle
```csharp
public class SandboxLifecycleManager
{
    private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(30);
    private readonly Timer _cleanupTimer;
    
    public async Task ManageLifecycle()
    {
        // Start containers on demand
        OnProjectOpen += async (project) => {
            if (!_sandboxes.ContainsKey(project.Id))
                await CreateSandbox(project);
        };
        
        // Stop idle containers
        _cleanupTimer = new Timer(async _ => {
            var idleSandboxes = _sandboxes
                .Where(s => DateTime.UtcNow - s.LastActivity > _idleTimeout);
            
            foreach (var sandbox in idleSandboxes)
            {
                await StopSandbox(sandbox.Id);
                _sandboxes.Remove(sandbox.Id);
            }
        }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        
        // Cleanup on project close
        OnProjectClose += async (project) => {
            if (project.KeepSandboxRunning)
                return;
                
            await StopSandbox(project.Id);
        };
    }
}
```

### Container Pool Management
```yaml
# container-pool.yaml
pool:
  warm_containers: 3  # Pre-started containers ready for use
  max_containers: 20  # Maximum concurrent sandboxes
  startup_timeout: 30s
  shutdown_timeout: 10s
  
  preload:
    - image: codeagent/sandbox:latest
      count: 2
    - image: codeagent/sandbox:nodejs
      count: 1
      
  resource_limits:
    total_memory: 32G
    total_cpus: 16
    per_container_max_memory: 8G
    per_container_max_cpus: 4
```

## Monitoring & Debugging

### Sandbox Monitoring
```csharp
public class SandboxMonitor
{
    public async Task<SandboxMetrics> GetMetrics(string projectId)
    {
        var sandbox = _sandboxes[projectId];
        var stats = await _docker.Containers.GetContainerStatsAsync(sandbox.Id);
        
        return new SandboxMetrics
        {
            CpuUsage = CalculateCpuUsage(stats),
            MemoryUsage = stats.MemoryStats.Usage,
            MemoryLimit = stats.MemoryStats.Limit,
            NetworkRx = stats.Networks.Sum(n => n.Value.RxBytes),
            NetworkTx = stats.Networks.Sum(n => n.Value.TxBytes),
            DiskRead = stats.BlkioStats.IoServiceBytesRecursive
                ?.FirstOrDefault(s => s.Op == "Read")?.Value ?? 0,
            DiskWrite = stats.BlkioStats.IoServiceBytesRecursive
                ?.FirstOrDefault(s => s.Op == "Write")?.Value ?? 0,
            ProcessCount = stats.PidsStats.Current,
            Uptime = DateTime.UtcNow - sandbox.StartedAt
        };
    }
}
```

### Debug Access
```bash
# Connect to sandbox shell
codeagent sandbox shell my-app

# Stream sandbox logs
codeagent sandbox logs my-app --follow --tail 100

# Copy files from sandbox
codeagent sandbox cp my-app:/workspace/output.log ./output.log

# Port forward to sandbox service
codeagent sandbox port-forward my-app 3000:3000

# Inspect sandbox state
codeagent sandbox inspect my-app
```

## Integration with Agents

### Agent-to-Sandbox Communication
```csharp
public class AgentSandboxBridge
{
    public async Task<AgentResponse> ProcessRequest(AgentRequest request)
    {
        // Determine target sandbox
        var sandbox = await GetOrCreateSandbox(request.ProjectId);
        
        // Prepare MCP message
        var mcpMessage = new MCPMessage
        {
            Type = "tool_use",
            Tools = request.RequiredTools,
            Context = request.Context,
            Metadata = new {
                AgentType = request.AgentType,
                Provider = request.Provider,
                Model = request.Model
            }
        };
        
        // Execute in sandbox
        var response = await sandbox.ExecuteMCP(mcpMessage);
        
        // Process results
        return new AgentResponse
        {
            Success = response.Success,
            Output = response.Output,
            Files = response.GeneratedFiles,
            Metrics = response.ExecutionMetrics
        };
    }
}
```

### Workflow in Sandboxed Environment
```yaml
workflow:
  name: "Sandboxed Development"
  
  stages:
    - name: "Setup Sandbox"
      action: sandbox.create
      config:
        image: codeagent/sandbox:nodejs
        resources:
          memory: 4G
          cpus: 2
          
    - name: "Planning"
      agent: planning
      provider: anthropic/claude-3-opus
      execute_in: sandbox
      tools:
        - file.read
        - file.write
        - exec
        
    - name: "Implementation"
      agent: coding
      provider: openai/gpt-4
      execute_in: sandbox
      tools:
        - file.write
        - exec
        - git
        
    - name: "Testing"
      agent: testing
      provider: gemini/gemini-pro
      execute_in: sandbox
      tools:
        - exec
        - file.read
        
    - name: "Build & Package"
      action: sandbox.exec
      commands:
        - npm run build
        - npm run package
        
    - name: "Extract Results"
      action: sandbox.copy
      source: /workspace/dist
      destination: ./output
```

---
*Document Version: 1.0*  
*Last Updated: [Current Date]*  
*Docker Desktop MCP: Beta Support*  
*Docker LLM: Beta Support*