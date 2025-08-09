namespace CodeAgent.Agents;

public class AgentRequest
{
    public string                    RequestId  { get; set; } = Guid.NewGuid().ToString();
    public string                    Command    { get; set; } = string.Empty;
    public string                    Content    { get; set; } = string.Empty;
    public AgentContext              Context    { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public TimeSpan?                 Timeout    { get; set; }
}