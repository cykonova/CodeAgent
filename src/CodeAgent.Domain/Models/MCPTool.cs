namespace CodeAgent.Domain.Models;

public class MCPTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, MCPParameter> Parameters { get; set; } = new();
}