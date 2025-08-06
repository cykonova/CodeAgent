namespace CodeAgent.Domain.Interfaces;

public interface ITeamService
{
    Task<Team> CreateTeamAsync(string name, string ownerId, CancellationToken cancellationToken = default);
    Task<Team?> GetTeamAsync(string teamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Team>> GetUserTeamsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> AddMemberAsync(string teamId, string userId, TeamRole role, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMemberRoleAsync(string teamId, string userId, TeamRole newRole, CancellationToken cancellationToken = default);
    Task<TeamInvite> CreateInviteAsync(string teamId, string email, string invitedBy, CancellationToken cancellationToken = default);
    Task<bool> AcceptInviteAsync(string inviteCode, string userId, CancellationToken cancellationToken = default);
    Task<SharedResource> ShareResourceAsync(string teamId, string resourceId, ResourceType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<SharedResource>> GetSharedResourcesAsync(string teamId, CancellationToken cancellationToken = default);
}

public class Team
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public List<TeamMember> Members { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class TeamMember
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public TeamRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public enum TeamRole
{
    Member,
    Admin,
    Owner
}

public class TeamInvite
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TeamId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string InviteCode { get; set; } = Guid.NewGuid().ToString();
    public string InvitedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public string? UsedBy { get; set; }
}

public class SharedResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TeamId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public string SharedBy { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ResourceType
{
    Session,
    Configuration,
    Plugin,
    Context,
    Template
}