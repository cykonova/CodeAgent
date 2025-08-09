namespace CodeAgent.Sandbox.MCP;

public class McpResponse
{
    public bool    Success { get; set; }
    public object? Result  { get; set; }
    public string? Error   { get; set; }
}