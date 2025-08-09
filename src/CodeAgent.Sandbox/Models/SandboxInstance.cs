namespace CodeAgent.Sandbox.Models;

public class SandboxInstance
{
    public string                      Id           { get; set; } = string.Empty;
    public string                      Name         { get; set; } = string.Empty;
    public string                      ContainerId  { get; set; } = string.Empty;
    public string                      WorkspacePath { get; set; } = string.Empty;
    public SandboxStatus               Status       { get; set; }
    public DateTime                    CreatedAt    { get; set; }
    public DateTime?                   StartedAt    { get; set; }
    public Dictionary<int, int>        PortMappings { get; set; } = new();
    public Dictionary<string, string>  Environment  { get; set; } = new();
}