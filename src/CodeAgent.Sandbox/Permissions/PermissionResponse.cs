namespace CodeAgent.Sandbox.Permissions;

public class PermissionResponse
{
    public bool      IsApproved { get; set; }
    public string?   Reason     { get; set; }
    public TimeSpan? ValidFor   { get; set; }
}