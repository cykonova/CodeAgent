namespace CodeAgent.Agents;

public class AgentArtifact
{
    public string                    Id       { get; set; } = Guid.NewGuid().ToString();
    public string                    Name     { get; set; } = string.Empty;
    public ArtifactType              Type     { get; set; }
    public string                    Content  { get; set; } = string.Empty;
    public string                    FilePath { get; set; } = string.Empty;
    public string                    Language { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}