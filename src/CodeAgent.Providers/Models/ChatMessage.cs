namespace CodeAgent.Providers.Models;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Name { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}