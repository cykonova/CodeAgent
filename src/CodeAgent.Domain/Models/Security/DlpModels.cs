namespace CodeAgent.Domain.Models.Security;

public class DlpScanResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime ScanTimestamp { get; set; } = DateTime.UtcNow;
    public bool HasSensitiveData { get; set; }
    public List<SensitiveDataFinding> Findings { get; set; } = new();
    public DataClassification? Classification { get; set; }
    public Dictionary<string, int> FindingCounts { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class SensitiveDataFinding
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string RedactedValue { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public SensitivityLevel Sensitivity { get; set; }
    public string Context { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
}

public enum SensitivityLevel
{
    Low,
    Medium,
    High,
    Critical
}

public class DataClassification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ClassificationLevel Level { get; set; }
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, double> ConfidenceScores { get; set; } = new();
    public bool RequiresEncryption { get; set; }
    public bool RequiresApproval { get; set; }
    public string HandlingInstructions { get; set; } = string.Empty;
}

public enum ClassificationLevel
{
    Public,
    Internal,
    Confidential,
    Restricted,
    TopSecret
}

public enum RedactionLevel
{
    Partial,
    Full,
    Smart
}

public class DlpPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DlpRule> Rules { get; set; } = new();
    public PolicyAction Action { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}

public class DlpRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DlpRuleType Type { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public SensitivityLevel Sensitivity { get; set; }
    public List<string> Keywords { get; set; } = new();
    public Dictionary<string, object> Conditions { get; set; } = new();
}

public enum DlpRuleType
{
    Regex,
    Keyword,
    DataType,
    Custom
}

public enum PolicyAction
{
    Allow,
    Warn,
    Block,
    Redact,
    Encrypt,
    Notify
}

public class DlpReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalScans { get; set; }
    public int TotalFindings { get; set; }
    public Dictionary<string, int> FindingsByType { get; set; } = new();
    public Dictionary<SensitivityLevel, int> FindingsBySensitivity { get; set; } = new();
    public List<DlpIncident> Incidents { get; set; } = new();
    public List<string> TopViolators { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
}

public class DlpIncident
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SensitivityLevel Severity { get; set; }
    public string PolicyViolated { get; set; } = string.Empty;
    public PolicyAction ActionTaken { get; set; }
    public bool Resolved { get; set; }
    public string Resolution { get; set; } = string.Empty;
}

public static class SensitiveDataPatterns
{
    // Common patterns for sensitive data detection
    public const string CreditCard = @"\b(?:\d[ -]*?){13,16}\b";
    public const string SSN = @"\b\d{3}-\d{2}-\d{4}\b";
    public const string Email = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
    public const string Phone = @"\b(?:\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b";
    public const string ApiKey = @"\b(sk-|api[_-]?key[:\s]+)?[A-Za-z0-9]{16,}\b";
    public const string PrivateKey = @"-----BEGIN (RSA |EC )?PRIVATE KEY-----";
    public const string AWSAccessKey = @"AKIA[0-9A-Z]{16}";
    public const string AWSSecretKey = @"[0-9a-zA-Z/+=]{40}";
    public const string GitHubToken = @"ghp_[0-9a-zA-Z]{36}";
    public const string JwtToken = @"eyJ[A-Za-z0-9-_=]+\.eyJ[A-Za-z0-9-_=]+\.[A-Za-z0-9-_.+/=]+";
}