namespace CodeAgent.Sandbox.MCP;

public class McpServerInfo
{
    public string              Name         { get; set; } = string.Empty;
    public string              Version      { get; set; } = string.Empty;
    public List<McpCapability> Capabilities { get; set; } = new();
}