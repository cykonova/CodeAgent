namespace CodeAgent.Domain.Models.Security;

public class ThreatAnalysisResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    public ThreatLevel ThreatLevel { get; set; }
    public List<ThreatIndicator> Indicators { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = new();
    public bool RequiresImmediateAction { get; set; }
}

public enum ThreatLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public class ThreatIndicator
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Severity { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SecurityIncident
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public ThreatLevel Severity { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AffectedUser { get; set; } = string.Empty;
    public List<string> AffectedResources { get; set; } = new();
    public List<IncidentResponse> Responses { get; set; } = new();
    public Dictionary<string, object> Evidence { get; set; } = new();
}

public enum IncidentStatus
{
    Open,
    InProgress,
    Contained,
    Resolved,
    Closed
}

public class IncidentResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ResponseAction Action { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ResponderId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum ResponseAction
{
    Investigate,
    Contain,
    Isolate,
    Block,
    Remediate,
    Escalate,
    Close
}

public class ThreatIntelligence
{
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ThreatPattern> KnownPatterns { get; set; } = new();
    public List<string> BlockedIPs { get; set; } = new();
    public List<string> MaliciousHashes { get; set; } = new();
    public List<string> SuspiciousUrls { get; set; } = new();
    public Dictionary<string, ThreatLevel> ThreatActors { get; set; } = new();
}

public class ThreatPattern
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public ThreatLevel Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Mitigations { get; set; } = new();
}

public class AnomalyDetectionResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public List<Anomaly> Anomalies { get; set; } = new();
    public double AnomalyScore { get; set; }
    public bool RequiresReview { get; set; }
}

public class Anomaly
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Deviation { get; set; }
    public string ExpectedBehavior { get; set; } = string.Empty;
    public string ObservedBehavior { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class RiskAssessment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public double RiskScore { get; set; }
    public List<RiskFactor> Factors { get; set; } = new();
    public bool Approved { get; set; }
    public string? ApprovalReason { get; set; }
}

public enum RiskLevel
{
    Negligible,
    Low,
    Medium,
    High,
    Extreme
}

public class RiskFactor
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Score { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class MalwareSignature
{
    public string Hash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ThreatLevel Severity { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}