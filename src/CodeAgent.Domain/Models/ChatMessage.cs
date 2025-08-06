namespace CodeAgent.Domain.Models;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public ChatMessage() { }
    
    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}