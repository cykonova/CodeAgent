namespace CodeAgent.Domain.Models.Security;

public class AuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public AuditEventType EventType { get; set; }
    public string EventCategory { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public enum AuditEventType
{
    Authentication,
    Authorization,
    FileAccess,
    FileModification,
    ConfigurationChange,
    SecurityPolicyChange,
    ProviderAccess,
    SessionManagement,
    DataAccess,
    SystemEvent
}

public enum AuditSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

public enum SecurityEventType
{
    LoginSuccess,
    LoginFailure,
    LogoutSuccess,
    MfaSuccess,
    MfaFailure,
    PermissionDenied,
    PolicyViolation,
    SuspiciousActivity,
    SecurityAlert,
    SessionTimeout
}

public enum FileOperation
{
    Read,
    Write,
    Delete,
    Create,
    Rename,
    Move,
    Copy,
    Execute
}

public class AuditReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<AuditEventType, int> EventsByType { get; set; } = new();
    public Dictionary<AuditSeverity, int> EventsBySeverity { get; set; } = new();
    public List<AuditEntry> CriticalEvents { get; set; } = new();
    public List<UserActivity> TopUsers { get; set; } = new();
    public List<ResourceAccess> TopResources { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
}

public class UserActivity
{
    public string UserId { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public DateTime LastActivity { get; set; }
    public List<string> TopActions { get; set; } = new();
}

public class ResourceAccess
{
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public int AccessCount { get; set; }
    public List<string> TopUsers { get; set; } = new();
}

public enum ComplianceStandard
{
    SOC2,
    ISO27001,
    HIPAA,
    GDPR,
    PCI_DSS,
    NIST
}

public class ComplianceReport
{
    public ComplianceStandard Standard { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public ComplianceStatus OverallStatus { get; set; }
    public List<ComplianceControl> Controls { get; set; } = new();
    public List<ComplianceViolation> Violations { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public enum ComplianceStatus
{
    Compliant,
    PartiallyCompliant,
    NonCompliant,
    NotApplicable
}

public class ComplianceControl
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceStatus Status { get; set; }
    public List<string> Evidence { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public class ComplianceViolation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string ControlId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AuditSeverity Severity { get; set; }
    public string RemediationSteps { get; set; } = string.Empty;
    public bool Resolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}