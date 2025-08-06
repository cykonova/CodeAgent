namespace CodeAgent.Domain.Interfaces;

public enum PermissionResult
{
    Denied,
    Allowed,
    AllowedForAll
}

public interface IPermissionService
{
    Task<bool> RequestPermissionAsync(string operation, string path, string? details = null);
    bool IsPathAllowed(string path);
    void SetWorkingDirectory(string path);
    void SetProjectDirectory(string path);
    string GetSafePath(string requestedPath);
    bool IsOutsideUserProfile(string path);
}