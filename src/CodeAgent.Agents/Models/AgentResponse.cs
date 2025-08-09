namespace CodeAgent.Agents;

public class AgentResponse
{
    public string                    ResponseId     { get; set; } = Guid.NewGuid().ToString();
    public string                    RequestId      { get; set; } = string.Empty;
    public string                    AgentId        { get; set; } = string.Empty;
    public bool                      Success        { get; set; }
    public string                    Content        { get; set; } = string.Empty;
    public AgentContext              UpdatedContext { get; set; } = new();
    public List<AgentArtifact>       Artifacts      { get; set; } = new();
    public string?                   ErrorMessage   { get; set; }
    public Dictionary<string, object> Metadata       { get; set; } = new();
    public TimeSpan                  ExecutionTime  { get; set; }
}