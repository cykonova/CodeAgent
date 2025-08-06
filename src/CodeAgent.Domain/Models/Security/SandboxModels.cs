namespace CodeAgent.Domain.Models.Security;

public class SandboxEnvironment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public SandboxType Type { get; set; }
    public SandboxStatus Status { get; set; } = SandboxStatus.Creating;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DestroyedAt { get; set; }
    public ResourceLimits ResourceLimits { get; set; } = new();
    public NetworkIsolationLevel NetworkIsolation { get; set; } = NetworkIsolationLevel.Restricted;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum SandboxType
{
    Container,
    VirtualMachine,
    WebAssembly,
    Process,
    FileSystem
}

public enum SandboxStatus
{
    Creating,
    Ready,
    Running,
    Suspended,
    Destroying,
    Destroyed,
    Error
}

public class SandboxConfiguration
{
    public string Name { get; set; } = string.Empty;
    public SandboxType Type { get; set; } = SandboxType.Container;
    public ResourceLimits ResourceLimits { get; set; } = new();
    public NetworkIsolationLevel NetworkIsolation { get; set; } = NetworkIsolationLevel.Restricted;
    public TimeSpan? MaxExecutionTime { get; set; }
    public List<string> AllowedCommands { get; set; } = new();
    public List<string> BlockedCommands { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

public class ResourceLimits
{
    public long MaxMemoryBytes { get; set; } = 512 * 1024 * 1024; // 512MB default
    public int MaxCpuPercent { get; set; } = 50; // 50% CPU default
    public long MaxDiskBytes { get; set; } = 1024 * 1024 * 1024; // 1GB default
    public int MaxProcesses { get; set; } = 10;
    public int MaxFileHandles { get; set; } = 100;
    public long MaxNetworkBandwidthBytesPerSecond { get; set; } = 1024 * 1024; // 1MB/s default
}

public class ResourceUsage
{
    public long MemoryUsedBytes { get; set; }
    public double CpuUsagePercent { get; set; }
    public long DiskUsedBytes { get; set; }
    public int ActiveProcesses { get; set; }
    public int OpenFileHandles { get; set; }
    public long NetworkBytesReceived { get; set; }
    public long NetworkBytesSent { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

public enum NetworkIsolationLevel
{
    None,           // No network isolation
    Restricted,     // Limited to specific ports/IPs
    LocalOnly,      // Only local network access
    Complete        // No network access
}

public class ExecutionResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SandboxId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public ResourceUsage ResourceUsage { get; set; } = new();
    public List<SecurityViolation> SecurityViolations { get; set; } = new();
}

public class SecurityViolation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public ViolationSeverity Severity { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public enum ViolationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public class FilesystemVirtualization
{
    public string RootPath { get; set; } = string.Empty;
    public List<MountPoint> MountPoints { get; set; } = new();
    public List<string> ReadOnlyPaths { get; set; } = new();
    public List<string> BlockedPaths { get; set; } = new();
    public bool EnableCopyOnWrite { get; set; } = true;
}

public class MountPoint
{
    public string HostPath { get; set; } = string.Empty;
    public string SandboxPath { get; set; } = string.Empty;
    public bool ReadOnly { get; set; }
    public bool Hidden { get; set; }
}