using System.Security.Cryptography;
using System.Text;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Security;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Infrastructure.Services;

public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private readonly Dictionary<string, Role> _roles = new();
    private readonly Dictionary<string, List<string>> _userRoles = new();
    private readonly Dictionary<string, SecurityPolicy> _policies = new();
    private readonly Dictionary<string, SecuritySession> _sessions = new();
    private readonly Dictionary<string, string> _mfaSecrets = new();

    public SecurityService(ILogger<SecurityService> logger)
    {
        _logger = logger;
        InitializeSystemRoles();
    }

    private void InitializeSystemRoles()
    {
        // Administrator role
        var adminRole = new Role
        {
            Id = SystemRoles.Administrator,
            Name = "Administrator",
            Description = "Full system access",
            IsSystem = true,
            Permissions = new List<Permission>
            {
                new() { Name = SystemPermissions.FileRead, Resource = "*", Action = "read" },
                new() { Name = SystemPermissions.FileWrite, Resource = "*", Action = "write" },
                new() { Name = SystemPermissions.FileDelete, Resource = "*", Action = "delete" },
                new() { Name = SystemPermissions.ProviderManage, Resource = "*", Action = "manage" },
                new() { Name = SystemPermissions.SecurityManage, Resource = "*", Action = "manage" },
                new() { Name = SystemPermissions.PluginManage, Resource = "*", Action = "manage" }
            }
        };
        _roles[adminRole.Id] = adminRole;

        // Developer role
        var devRole = new Role
        {
            Id = SystemRoles.Developer,
            Name = "Developer",
            Description = "Standard development access",
            IsSystem = true,
            Permissions = new List<Permission>
            {
                new() { Name = SystemPermissions.FileRead, Resource = "*", Action = "read" },
                new() { Name = SystemPermissions.FileWrite, Resource = "*", Action = "write" },
                new() { Name = SystemPermissions.ProviderUse, Resource = "*", Action = "use" },
                new() { Name = SystemPermissions.SessionCreate, Resource = "self", Action = "create" },
                new() { Name = SystemPermissions.PluginExecute, Resource = "*", Action = "execute" }
            }
        };
        _roles[devRole.Id] = devRole;

        // Reviewer role
        var reviewRole = new Role
        {
            Id = SystemRoles.Reviewer,
            Name = "Reviewer",
            Description = "Read-only review access",
            IsSystem = true,
            Permissions = new List<Permission>
            {
                new() { Name = SystemPermissions.FileRead, Resource = "*", Action = "read" },
                new() { Name = SystemPermissions.SecurityAudit, Resource = "*", Action = "read" }
            }
        };
        _roles[reviewRole.Id] = reviewRole;

        // ReadOnly role
        var readOnlyRole = new Role
        {
            Id = SystemRoles.ReadOnly,
            Name = "ReadOnly",
            Description = "Minimal read-only access",
            IsSystem = true,
            Permissions = new List<Permission>
            {
                new() { Name = SystemPermissions.FileRead, Resource = "public", Action = "read" }
            }
        };
        _roles[readOnlyRole.Id] = readOnlyRole;
    }

    public Task<Role?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        _roles.TryGetValue(roleId, out var role);
        return Task.FromResult(role);
    }

    public Task<IEnumerable<Role>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_roles.Values.AsEnumerable());
    }

    public Task<Role> CreateRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (_roles.ContainsKey(role.Id))
            throw new InvalidOperationException($"Role with ID {role.Id} already exists");

        _roles[role.Id] = role;
        _logger.LogInformation("Created role {RoleId} with {PermissionCount} permissions", 
            role.Id, role.Permissions.Count);
        
        return Task.FromResult(role);
    }

    public Task<Role> UpdateRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (!_roles.ContainsKey(role.Id))
            throw new InvalidOperationException($"Role with ID {role.Id} not found");

        if (_roles[role.Id].IsSystem)
            throw new InvalidOperationException("Cannot modify system roles");

        role.ModifiedAt = DateTime.UtcNow;
        _roles[role.Id] = role;
        _logger.LogInformation("Updated role {RoleId}", role.Id);
        
        return Task.FromResult(role);
    }

    public Task<bool> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (!_roles.TryGetValue(roleId, out var role))
            return Task.FromResult(false);

        if (role.IsSystem)
            throw new InvalidOperationException("Cannot delete system roles");

        _roles.Remove(roleId);
        
        // Remove role from all users
        foreach (var userRoles in _userRoles.Values)
        {
            userRoles.Remove(roleId);
        }
        
        _logger.LogInformation("Deleted role {RoleId}", roleId);
        return Task.FromResult(true);
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        return userRoles.Any(r => r.Permissions.Any(p => p.Name == permission));
    }

    public async Task<bool> HasAnyPermissionAsync(string userId, string[] permissions, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        var userPermissions = userRoles.SelectMany(r => r.Permissions).Select(p => p.Name).Distinct();
        return permissions.Any(p => userPermissions.Contains(p));
    }

    public async Task<bool> HasAllPermissionsAsync(string userId, string[] permissions, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        var userPermissions = userRoles.SelectMany(r => r.Permissions).Select(p => p.Name).Distinct().ToHashSet();
        return permissions.All(p => userPermissions.Contains(p));
    }

    public Task AssignRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        if (!_roles.ContainsKey(roleId))
            throw new InvalidOperationException($"Role {roleId} not found");

        if (!_userRoles.ContainsKey(userId))
            _userRoles[userId] = new List<string>();

        if (!_userRoles[userId].Contains(roleId))
        {
            _userRoles[userId].Add(roleId);
            _logger.LogInformation("Assigned role {RoleId} to user {UserId}", roleId, userId);
        }

        return Task.CompletedTask;
    }

    public Task RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        if (_userRoles.TryGetValue(userId, out var roles))
        {
            if (roles.Remove(roleId))
            {
                _logger.LogInformation("Removed role {RoleId} from user {UserId}", roleId, userId);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<Role>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!_userRoles.TryGetValue(userId, out var roleIds))
            return Task.FromResult(Enumerable.Empty<Role>());

        var roles = roleIds
            .Where(id => _roles.ContainsKey(id))
            .Select(id => _roles[id]);

        return Task.FromResult(roles);
    }

    public Task<SecurityPolicy?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default)
    {
        _policies.TryGetValue(policyId, out var policy);
        return Task.FromResult(policy);
    }

    public Task<IEnumerable<SecurityPolicy>> GetPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_policies.Values.Where(p => p.IsActive).AsEnumerable());
    }

    public Task<SecurityPolicy> CreatePolicyAsync(SecurityPolicy policy, CancellationToken cancellationToken = default)
    {
        if (_policies.ContainsKey(policy.Id))
            throw new InvalidOperationException($"Policy with ID {policy.Id} already exists");

        _policies[policy.Id] = policy;
        _logger.LogInformation("Created security policy {PolicyId} of type {PolicyType}", 
            policy.Id, policy.Type);
        
        return Task.FromResult(policy);
    }

    public Task<bool> ApplyPolicyAsync(string policyId, CancellationToken cancellationToken = default)
    {
        if (!_policies.TryGetValue(policyId, out var policy))
            return Task.FromResult(false);

        policy.IsActive = true;
        _logger.LogInformation("Applied security policy {PolicyId}", policyId);
        
        return Task.FromResult(true);
    }

    public Task<SecuritySession> CreateSessionAsync(string userId, CancellationToken cancellationToken = default)
    {
        var session = new SecuritySession
        {
            UserId = userId,
            Token = GenerateSecureToken(),
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            RequiresMfa = _mfaSecrets.ContainsKey(userId)
        };

        _sessions[session.Token] = session;
        _logger.LogInformation("Created session for user {UserId}", userId);
        
        return Task.FromResult(session);
    }

    public Task<bool> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionToken, out var session))
            return Task.FromResult(false);

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _sessions.Remove(sessionToken);
            return Task.FromResult(false);
        }

        if (session.RequiresMfa && !session.MfaCompleted)
            return Task.FromResult(false);

        session.LastActivityAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task InvalidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (_sessions.Remove(sessionToken))
        {
            _logger.LogInformation("Invalidated session {SessionToken}", sessionToken);
        }
        
        return Task.CompletedTask;
    }

    public Task<SecuritySession?> GetSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionToken, out var session);
        return Task.FromResult(session);
    }

    public Task<bool> IsMfaEnabledAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_mfaSecrets.ContainsKey(userId));
    }

    public Task<string> GenerateMfaTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Generate a 6-digit TOTP token (simplified for this implementation)
        var random = new Random();
        var token = random.Next(100000, 999999).ToString();
        _logger.LogInformation("Generated MFA token for user {UserId}", userId);
        return Task.FromResult(token);
    }

    public Task<bool> ValidateMfaTokenAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        // Simplified validation - in production, use proper TOTP
        _logger.LogInformation("Validated MFA token for user {UserId}", userId);
        return Task.FromResult(token.Length == 6 && int.TryParse(token, out _));
    }

    public Task EnableMfaAsync(string userId, string secret, CancellationToken cancellationToken = default)
    {
        _mfaSecrets[userId] = secret;
        _logger.LogInformation("Enabled MFA for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task DisableMfaAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_mfaSecrets.Remove(userId))
        {
            _logger.LogInformation("Disabled MFA for user {UserId}", userId);
        }
        return Task.CompletedTask;
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}