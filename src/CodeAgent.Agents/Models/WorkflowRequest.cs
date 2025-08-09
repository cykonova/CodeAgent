namespace CodeAgent.Agents;

public class WorkflowRequest
{
    public string                    RequestId         { get; set; } = Guid.NewGuid().ToString();
    public string                    Content           { get; set; } = string.Empty;
    public string                    SessionId         { get; set; } = string.Empty;
    public string                    ProjectId         { get; set; } = string.Empty;
    public string                    WorkingDirectory  { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters        { get; set; } = new();
}