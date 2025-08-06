namespace CodeAgent.Domain.Models;

public class ChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int? TokensUsed { get; set; }
    public bool IsComplete { get; set; } = true;
    public string? Error { get; set; }
}