namespace CodeAgent.Sandbox.MCP;

public class McpRequest
{
    public string                      Method     { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public string?                     SandboxId  { get; set; }
}