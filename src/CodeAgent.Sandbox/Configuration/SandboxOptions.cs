namespace CodeAgent.Sandbox.Configuration;

public class SandboxOptions
{
    public const string SectionName = "Sandbox";

    public SecurityLevel  SecurityLevel     { get; set; } = SecurityLevel.Container;
    public ResourceLimits Resources         { get; set; } = new();
    public NetworkOptions Network           { get; set; } = new();
    public string         WorkspaceBasePath { get; set; } = "/var/codeagent/workspaces";
    public string         DockerImage       { get; set; } = "codeagent/sandbox:latest";
    public TimeSpan       ContainerTimeout  { get; set; } = TimeSpan.FromMinutes(30);
}