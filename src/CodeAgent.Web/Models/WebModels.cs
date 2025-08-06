using CodeAgent.Domain.Models;

namespace CodeAgent.Web.Models;

public class AgentStatus
{
    public bool IsOnline { get; set; }
    public string? CurrentProvider { get; set; }
    public List<string> AvailableProviders { get; set; } = new();
    public int ActiveSessions { get; set; }
    public ProjectContext? CurrentContext { get; set; }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class BuildContextRequest
{
    public string ProjectPath { get; set; } = string.Empty;
}

public class SwitchProviderRequest
{
    public string ProviderName { get; set; } = string.Empty;
}

public class TeamInvitation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TeamId { get; set; } = string.Empty;
    public string InvitedEmail { get; set; } = string.Empty;
    public string InvitedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsAccepted { get; set; }
}

public class SharedSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string SharedBy { get; set; } = string.Empty;
    public List<string> SharedWith { get; set; } = new();
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    public SessionPermission Permission { get; set; }
}

public enum SessionPermission
{
    ReadOnly,
    Collaborate,
    FullControl
}

public class ExportRequest
{
    public string SessionId { get; set; } = string.Empty;
    public ExportFormat Format { get; set; }
    public bool IncludeMetadata { get; set; }
    public bool IncludeContext { get; set; }
}

public enum ExportFormat
{
    Json,
    Markdown,
    Html,
    Pdf
}

public class ImportRequest
{
    public string Content { get; set; } = string.Empty;
    public ExportFormat Format { get; set; }
    public bool CreateNewSession { get; set; }
}

public class AnalyticsData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; set; } = new();
    public Dictionary<string, string> Dimensions { get; set; } = new();
}

public class TelemetryData
{
    public string SessionId { get; set; } = string.Empty;
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public int ActiveConnections { get; set; }
    public Dictionary<string, long> ResponseTimes { get; set; } = new();
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

public class ConfigurationProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Settings { get; set; } = new();
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}