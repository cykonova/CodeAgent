namespace CodeAgent.Sandbox.Permissions;

public class PermissionRequest
{
    public Guid             Id             { get; set; } = Guid.NewGuid();
    public string           SandboxId      { get; set; } = string.Empty;
    public PermissionType   Type           { get; set; }
    public string           Resource       { get; set; } = string.Empty;
    public string           Reason         { get; set; } = string.Empty;
    public DateTime         RequestedAt    { get; set; } = DateTime.UtcNow;
    public PermissionStatus Status         { get; set; } = PermissionStatus.Pending;
    public string?          ResponseReason { get; set; }
    public DateTime?        RespondedAt    { get; set; }
}