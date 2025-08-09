namespace CodeAgent.Sandbox.Services;

public class AgentRunRequest
{
    public string                    AgentId        { get; set; } = string.Empty;
    public string                    SandboxId      { get; set; } = string.Empty;
    public string                    Task           { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters   { get; set; }
    public bool                      NonInteractive { get; set; } = true;
}