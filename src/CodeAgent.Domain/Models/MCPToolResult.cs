namespace CodeAgent.Domain.Models;

public class MCPToolResult
{
    public bool Success { get; set; }
    public string? ToolName { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public object? Data { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}