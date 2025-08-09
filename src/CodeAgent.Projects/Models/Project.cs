namespace CodeAgent.Projects.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectType Type { get; set; } = ProjectType.Standard;
    public string? TemplateName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ProjectConfiguration Configuration { get; set; } = new();
    public ProjectState State { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ProjectType
{
    Standard,
    Fast,
    Quality,
    Budget,
    Custom
}