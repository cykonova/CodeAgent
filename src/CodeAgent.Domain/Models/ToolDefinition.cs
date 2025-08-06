namespace CodeAgent.Domain.Models;

public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, ParameterDefinition> Parameters { get; set; } = new();
}

public class ParameterDefinition
{
    public string Type { get; set; } = "string";
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; } = false;
    public object? Default { get; set; }
}

public class ToolCall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
}

public class ToolResult
{
    public string ToolCallId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
}