namespace CodeAgent.Sandbox.MCP;

public class McpCapability
{
    public string                      Name        { get; set; } = string.Empty;
    public string                      Description { get; set; } = string.Empty;
    public Dictionary<string, object>? Schema      { get; set; }
}