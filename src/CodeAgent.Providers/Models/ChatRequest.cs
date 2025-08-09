namespace CodeAgent.Providers.Models;

public class ChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public double? TopP { get; set; }
    public string? SystemPrompt { get; set; }
    public bool Stream { get; set; }
    public Dictionary<string, object> CustomParameters { get; set; } = new();
}