namespace CodeAgent.Domain.Models;

public class ChatRequest
{
    public List<ChatMessage> Messages { get; set; } = new();
    public string? Model { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; } = false;
    public List<ToolDefinition>? Tools { get; set; }
    public string? ToolChoice { get; set; } // "auto", "none", or specific tool name
}