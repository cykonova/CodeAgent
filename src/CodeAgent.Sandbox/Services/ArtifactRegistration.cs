namespace CodeAgent.Sandbox.Services;

public class ArtifactRegistration
{
    public string                      Name     { get; set; } = string.Empty;
    public ArtifactType                Type     { get; set; }
    public string                      Path     { get; set; } = string.Empty;
    public int?                        Port     { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}