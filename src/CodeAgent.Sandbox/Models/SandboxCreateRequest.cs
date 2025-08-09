namespace CodeAgent.Sandbox.Models;

public class SandboxCreateRequest
{
    public string                      Name             { get; set; } = string.Empty;
    public string?                     AgentId          { get; set; }
    public Dictionary<string, string>? Environment      { get; set; }
    public int[]?                      RequiredPorts    { get; set; }
    public string?                     WorkspaceContent { get; set; }
}