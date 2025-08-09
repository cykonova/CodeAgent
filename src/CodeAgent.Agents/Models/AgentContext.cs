namespace CodeAgent.Agents;

public class AgentContext
{
    public string                    SessionId         { get; set; } = string.Empty;
    public string                    ProjectId         { get; set; } = string.Empty;
    public List<ContextMessage>      History           { get; set; } = new();
    public Dictionary<string, object> SharedState       { get; set; } = new();
    public List<string>              Files             { get; set; } = new();
    public string                    WorkingDirectory  { get; set; } = string.Empty;
    public int                       TokensUsed        { get; set; }
    public int                       TokensRemaining   { get; set; }
}