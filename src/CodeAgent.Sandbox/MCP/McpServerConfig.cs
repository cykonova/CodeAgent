namespace CodeAgent.Sandbox.MCP;

public class McpServerConfig
{
    public string                     ServerType  { get; set; } = string.Empty;
    public Dictionary<string, string> Environment { get; set; } = new();
    public int                        Port        { get; set; } = 9000;
}