# Sandbox Architecture

## Container Specification
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Development tools
RUN apt-get update && apt-get install -y \
    git nodejs npm python3 python3-pip \
    build-essential curl wget

# MCP executor
COPY agent-executor /usr/local/bin/
RUN chmod +x /usr/local/bin/agent-executor

# Working directory
WORKDIR /workspace

# MCP configuration
ENV MCP_MODE=sandbox
ENV MCP_PORT=8500

ENTRYPOINT ["agent-executor"]
```

## Security Profiles
```yaml
security_levels:
  full_isolation:
    network: none
    capabilities: []
    readonly_root: true
    
  development:
    network: bridge
    capabilities: [CHOWN, SETUID, SETGID]
    readonly_root: false
    
  production:
    network: custom
    capabilities: [ALL]
    readonly_root: false
```

## Resource Limits
```csharp
public class ResourceLimits
{
    public long Memory { get; set; } = 4_294_967_296; // 4GB
    public int CpuShares { get; set; } = 1024;
    public int CpuQuota { get; set; } = 100000;
    public long DiskQuota { get; set; } = 10_737_418_240; // 10GB
}
```

## Lifecycle Management
- Pre-warmed container pool
- Automatic cleanup
- Health monitoring
- Resource tracking