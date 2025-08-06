using CodeAgent.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CodeAgent.Web.Services;

public class TeamService : ITeamService
{
    private readonly ILogger<TeamService> _logger;
    private readonly ConcurrentDictionary<string, Team> _teams = new();
    private readonly ConcurrentDictionary<string, TeamInvite> _invites = new();
    private readonly ConcurrentDictionary<string, List<SharedResource>> _sharedResources = new();

    public TeamService(ILogger<TeamService> logger)
    {
        _logger = logger;
    }

    public Task<Team> CreateTeamAsync(string name, string ownerId, CancellationToken cancellationToken = default)
    {
        var team = new Team
        {
            Name = name,
            OwnerId = ownerId,
            Members = new List<TeamMember>
            {
                new TeamMember
                {
                    UserId = ownerId,
                    UserName = ownerId, // Would get from user service
                    Role = TeamRole.Owner,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        _teams[team.Id] = team;
        _logger.LogInformation("Created team {TeamId} with owner {OwnerId}", team.Id, ownerId);

        return Task.FromResult(team);
    }

    public Task<Team?> GetTeamAsync(string teamId, CancellationToken cancellationToken = default)
    {
        _teams.TryGetValue(teamId, out var team);
        return Task.FromResult(team);
    }

    public Task<IEnumerable<Team>> GetUserTeamsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userTeams = _teams.Values
            .Where(t => t.Members.Any(m => m.UserId == userId && m.IsActive))
            .AsEnumerable();

        return Task.FromResult(userTeams);
    }

    public Task<bool> AddMemberAsync(string teamId, string userId, TeamRole role, CancellationToken cancellationToken = default)
    {
        if (!_teams.TryGetValue(teamId, out var team))
            return Task.FromResult(false);

        if (team.Members.Any(m => m.UserId == userId))
            return Task.FromResult(false); // Already a member

        team.Members.Add(new TeamMember
        {
            UserId = userId,
            UserName = userId, // Would get from user service
            Role = role,
            JoinedAt = DateTime.UtcNow
        });

        team.ModifiedAt = DateTime.UtcNow;
        _logger.LogInformation("Added member {UserId} to team {TeamId} with role {Role}", userId, teamId, role);

        return Task.FromResult(true);
    }

    public Task<bool> RemoveMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default)
    {
        if (!_teams.TryGetValue(teamId, out var team))
            return Task.FromResult(false);

        var member = team.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            return Task.FromResult(false);

        if (member.Role == TeamRole.Owner)
            return Task.FromResult(false); // Cannot remove owner

        member.IsActive = false;
        team.ModifiedAt = DateTime.UtcNow;
        _logger.LogInformation("Removed member {UserId} from team {TeamId}", userId, teamId);

        return Task.FromResult(true);
    }

    public Task<bool> UpdateMemberRoleAsync(string teamId, string userId, TeamRole newRole, CancellationToken cancellationToken = default)
    {
        if (!_teams.TryGetValue(teamId, out var team))
            return Task.FromResult(false);

        var member = team.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
        if (member == null)
            return Task.FromResult(false);

        if (member.Role == TeamRole.Owner && newRole != TeamRole.Owner)
        {
            // Ensure there's at least one owner
            if (!team.Members.Any(m => m.UserId != userId && m.Role == TeamRole.Owner))
                return Task.FromResult(false);
        }

        member.Role = newRole;
        team.ModifiedAt = DateTime.UtcNow;
        _logger.LogInformation("Updated member {UserId} role to {Role} in team {TeamId}", userId, newRole, teamId);

        return Task.FromResult(true);
    }

    public Task<TeamInvite> CreateInviteAsync(string teamId, string email, string invitedBy, CancellationToken cancellationToken = default)
    {
        if (!_teams.ContainsKey(teamId))
            throw new InvalidOperationException($"Team {teamId} not found");

        var invite = new TeamInvite
        {
            TeamId = teamId,
            Email = email,
            InvitedBy = invitedBy,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _invites[invite.InviteCode] = invite;
        _logger.LogInformation("Created invite for {Email} to team {TeamId}", email, teamId);

        return Task.FromResult(invite);
    }

    public Task<bool> AcceptInviteAsync(string inviteCode, string userId, CancellationToken cancellationToken = default)
    {
        if (!_invites.TryGetValue(inviteCode, out var invite))
            return Task.FromResult(false);

        if (invite.IsUsed || invite.ExpiresAt < DateTime.UtcNow)
            return Task.FromResult(false);

        invite.IsUsed = true;
        invite.UsedBy = userId;

        // Add user to team
        return AddMemberAsync(invite.TeamId, userId, TeamRole.Member, cancellationToken);
    }

    public Task<SharedResource> ShareResourceAsync(string teamId, string resourceId, ResourceType type, CancellationToken cancellationToken = default)
    {
        if (!_teams.ContainsKey(teamId))
            throw new InvalidOperationException($"Team {teamId} not found");

        var sharedResource = new SharedResource
        {
            TeamId = teamId,
            ResourceId = resourceId,
            Type = type,
            SharedBy = "current-user" // Would get from context
        };

        if (!_sharedResources.ContainsKey(teamId))
            _sharedResources[teamId] = new List<SharedResource>();

        _sharedResources[teamId].Add(sharedResource);
        _logger.LogInformation("Shared {Type} resource {ResourceId} with team {TeamId}", type, resourceId, teamId);

        return Task.FromResult(sharedResource);
    }

    public Task<IEnumerable<SharedResource>> GetSharedResourcesAsync(string teamId, CancellationToken cancellationToken = default)
    {
        if (_sharedResources.TryGetValue(teamId, out var resources))
            return Task.FromResult(resources.AsEnumerable());

        return Task.FromResult(Enumerable.Empty<SharedResource>());
    }
}