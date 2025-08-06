namespace CodeAgent.Domain.Models.Teams;

public class Team
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<TeamMember> Members { get; set; } = new();
    public List<SharedAgentConfig> SharedAgents { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}

public class TeamMember
{
    public string UserId { get; set; } = string.Empty;
    public TeamRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public enum TeamRole
{
    Owner,
    Admin,
    Member,
    Viewer
}

public class SharedAgentConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string SharedBy { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
}