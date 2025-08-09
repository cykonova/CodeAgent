# MCP (Model Context Protocol) Architecture

## MCP Message Structure
```typescript
interface MCPMessage {
  id: string;
  type: 'tool_use' | 'tool_result';
  metadata: Record<string, any>;
  context: SegmentedContext;
  tools: MCPTool[];
  instructions: SystemInstructions;
}

interface MCPTool {
  name: string;
  description: string;
  parameters: ToolSchema;
  executor: ToolExecutor;
}
```

## Tool Registration
```csharp
public class MCPServer
{
    private Dictionary<string, Func<ToolRequest, Task<ToolResult>>> _tools;
    
    public void RegisterTool(string name, Func<ToolRequest, Task<ToolResult>> handler)
    {
        _tools[name] = handler;
    }
    
    public async Task<ToolResult> ExecuteTool(string name, ToolRequest request)
    {
        return await _tools[name](request);
    }
}
```

## Built-in Tools
| Tool | Description | Sandbox Only |
|------|-------------|--------------|
| exec | Execute commands | Yes |
| file.read | Read files | No |
| file.write | Write files | Yes |
| git | Git operations | Yes |
| build | Build project | Yes |
| test | Run tests | Yes |

## Docker MCP Integration
```yaml
docker_mcp_tools:
  - docker.container.create
  - docker.container.exec
  - docker.compose.up
  - docker.image.build
  - docker.volume.create
```

## Security Model
- Tool permissions
- Execution context
- Resource limits
- Audit logging