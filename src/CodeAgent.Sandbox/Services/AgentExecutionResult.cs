namespace CodeAgent.Sandbox.Services;

public class AgentExecutionResult
{
    public string                      ExecutionId { get; set; } = string.Empty;
    public AgentExecutionStatus        Status      { get; set; }
    public string?                     Output      { get; set; }
    public string?                     Error       { get; set; }
    public Dictionary<string, object>? Artifacts   { get; set; }
    public TimeSpan                    Duration    { get; set; }
}