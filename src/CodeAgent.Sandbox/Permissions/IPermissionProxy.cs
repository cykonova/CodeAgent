namespace CodeAgent.Sandbox.Permissions;

public interface IPermissionProxy
{
    Task<PermissionResponse> RequestPermissionAsync(PermissionRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionRequest>> GetPendingRequestsAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task RevokePermissionAsync(Guid requestId, CancellationToken cancellationToken = default);
}