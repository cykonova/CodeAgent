namespace CodeAgent.Domain.Models.Security;

public class SecurityPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PolicyType Type { get; set; }
    public PolicyEnforcement Enforcement { get; set; } = PolicyEnforcement.Warning;
    public Dictionary<string, object> Rules { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public List<string> DeniedPermissions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum PolicyType
{
    Access,
    DataProtection,
    Compliance,
    Threat,
    Resource,
    Network
}

public enum PolicyEnforcement
{
    Disabled,
    Warning,
    Blocking,
    Strict
}

public class SecuritySession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool RequiresMfa { get; set; }
    public bool MfaCompleted { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}