namespace CodeAgent.Agents;

public class ContextMessage
{
    public string                    Id        { get; set; } = Guid.NewGuid().ToString();
    public string                    AgentId   { get; set; } = string.Empty;
    public string                    Role      { get; set; } = string.Empty;
    public string                    Content   { get; set; } = string.Empty;
    public DateTime                  Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata  { get; set; } = new();
}