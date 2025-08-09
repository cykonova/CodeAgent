namespace CodeAgent.Providers.Models;

public class ChatResponse
{
    public string Id { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public ChatMessage Message { get; set; } = new();
    public Usage? Usage { get; set; }
    public long CreatedAt { get; set; }
    public string? FinishReason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}