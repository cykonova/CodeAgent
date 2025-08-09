using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Sandbox.Permissions;

public class PermissionProxy : IPermissionProxy
{
    private readonly ILogger<PermissionProxy> _logger;
    private readonly ConcurrentDictionary<Guid, PermissionRequest> _requests = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _sandboxRequests = new();

    public event Func<PermissionRequest, Task<PermissionResponse>>? OnPermissionRequested;

    public PermissionProxy(ILogger<PermissionProxy> logger)
    {
        _logger = logger;
    }

    public async Task<PermissionResponse> RequestPermissionAsync(PermissionRequest request, CancellationToken cancellationToken = default)
    {
        _requests[request.Id] = request;
        
        if (!_sandboxRequests.ContainsKey(request.SandboxId))
        {
            _sandboxRequests[request.SandboxId] = new List<Guid>();
        }
        _sandboxRequests[request.SandboxId].Add(request.Id);

        _logger.LogInformation("Permission requested: {Type} for {Resource} in sandbox {SandboxId}", 
            request.Type, request.Resource, request.SandboxId);

        // If no handler is registered, auto-deny for security
        if (OnPermissionRequested == null)
        {
            request.Status = PermissionStatus.Denied;
            request.ResponseReason = "No permission handler configured";
            request.RespondedAt = DateTime.UtcNow;
            
            return new PermissionResponse
            {
                IsApproved = false,
                Reason = "No permission handler configured"
            };
        }

        try
        {
            // Invoke the permission handler (UI/CLI will handle this)
            var response = await OnPermissionRequested.Invoke(request);
            
            request.Status = response.IsApproved ? PermissionStatus.Approved : PermissionStatus.Denied;
            request.ResponseReason = response.Reason;
            request.RespondedAt = DateTime.UtcNow;

            _logger.LogInformation("Permission {Status}: {Type} for {Resource} in sandbox {SandboxId}", 
                request.Status, request.Type, request.Resource, request.SandboxId);

            return response;
        }
        catch (TaskCanceledException)
        {
            request.Status = PermissionStatus.Expired;
            request.ResponseReason = "Request timed out";
            request.RespondedAt = DateTime.UtcNow;
            
            return new PermissionResponse
            {
                IsApproved = false,
                Reason = "Request timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing permission request {RequestId}", request.Id);
            
            request.Status = PermissionStatus.Denied;
            request.ResponseReason = "Error processing request";
            request.RespondedAt = DateTime.UtcNow;
            
            return new PermissionResponse
            {
                IsApproved = false,
                Reason = "Error processing request"
            };
        }
    }

    public async Task<IEnumerable<PermissionRequest>> GetPendingRequestsAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        if (!_sandboxRequests.TryGetValue(sandboxId, out var requestIds))
        {
            return Enumerable.Empty<PermissionRequest>();
        }

        var pendingRequests = requestIds
            .Select(id => _requests.TryGetValue(id, out var req) ? req : null)
            .Where(req => req != null && req.Status == PermissionStatus.Pending)
            .Cast<PermissionRequest>();

        return await Task.FromResult(pendingRequests);
    }

    public async Task RevokePermissionAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        if (_requests.TryGetValue(requestId, out var request))
        {
            request.Status = PermissionStatus.Denied;
            request.ResponseReason = "Permission revoked";
            request.RespondedAt = DateTime.UtcNow;

            _logger.LogInformation("Permission revoked: {RequestId}", requestId);
        }

        await Task.CompletedTask;
    }
}