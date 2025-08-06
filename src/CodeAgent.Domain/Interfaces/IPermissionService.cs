namespace CodeAgent.Domain.Interfaces;

public interface IPermissionService
{
    Task<bool> RequestPermissionAsync(string operation, string path, string? details = null);
    bool IsPathAllowed(string path);
    void SetWorkingDirectory(string path);
    string GetSafePath(string requestedPath);
}