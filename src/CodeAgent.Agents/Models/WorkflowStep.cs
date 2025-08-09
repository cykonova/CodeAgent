namespace CodeAgent.Agents;

public class WorkflowStep
{
    public string                    Name          { get; set; } = string.Empty;
    public AgentType                 AgentType     { get; set; }
    public string                    Command       { get; set; } = string.Empty;
    public string?                   Content       { get; set; }
    public Dictionary<string, object> Parameters    { get; set; } = new();
    public bool                      IsRequired    { get; set; } = true;
    public bool                      AllowParallel { get; set; }
    public int                       Order         { get; set; }
    public TimeSpan?                 Timeout       { get; set; }
}