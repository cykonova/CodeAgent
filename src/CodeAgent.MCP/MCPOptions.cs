namespace CodeAgent.MCP;

public class MCPOptions
{
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableCaching { get; set; } = true;
    public List<string> DefaultServers { get; set; } = new();
}