namespace CodeAgent.Sandbox.Services;

public class ArtifactInfo
{
    public string                     Id         { get; set; } = string.Empty;
    public string                     SandboxId  { get; set; } = string.Empty;
    public string                     Name       { get; set; } = string.Empty;
    public ArtifactType               Type       { get; set; }
    public string                     Path       { get; set; } = string.Empty;
    public string?                    PreviewUrl { get; set; }
    public long                       Size       { get; set; }
    public DateTime                   CreatedAt  { get; set; }
    public Dictionary<string, string> Metadata   { get; set; } = new();
}