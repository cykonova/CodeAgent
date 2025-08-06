namespace CodeAgent.Domain.Models;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ToolCallId { get; set; } // For tool responses
    
    public ChatMessage() { }
    
    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
    
    public ChatMessage(string role, string content, string toolCallId)
    {
        Role = role;
        Content = content;
        ToolCallId = toolCallId;
    }
}