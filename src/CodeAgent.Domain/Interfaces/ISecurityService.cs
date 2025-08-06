using CodeAgent.Domain.Models.Security;

namespace CodeAgent.Domain.Interfaces;

public interface ISecurityService
{
    // RBAC operations
    Task<Role?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<Role> CreateRoleAsync(Role role, CancellationToken cancellationToken = default);
    Task<Role> UpdateRoleAsync(Role role, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);
    
    // Permission checking
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);
    Task<bool> HasAnyPermissionAsync(string userId, string[] permissions, CancellationToken cancellationToken = default);
    Task<bool> HasAllPermissionsAsync(string userId, string[] permissions, CancellationToken cancellationToken = default);
    
    // User-role assignment
    Task AssignRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    
    // Security policy
    Task<SecurityPolicy?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityPolicy>> GetPoliciesAsync(CancellationToken cancellationToken = default);
    Task<SecurityPolicy> CreatePolicyAsync(SecurityPolicy policy, CancellationToken cancellationToken = default);
    Task<bool> ApplyPolicyAsync(string policyId, CancellationToken cancellationToken = default);
    
    // Session management
    Task<SecuritySession> CreateSessionAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task InvalidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<SecuritySession?> GetSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    
    // MFA operations
    Task<bool> IsMfaEnabledAsync(string userId, CancellationToken cancellationToken = default);
    Task<string> GenerateMfaTokenAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateMfaTokenAsync(string userId, string token, CancellationToken cancellationToken = default);
    Task EnableMfaAsync(string userId, string secret, CancellationToken cancellationToken = default);
    Task DisableMfaAsync(string userId, CancellationToken cancellationToken = default);
}