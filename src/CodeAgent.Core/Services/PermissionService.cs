using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Core.Services;

public class PermissionService : IPermissionService
{
    private readonly ILogger<PermissionService> _logger;
    private readonly IPermissionPrompt _permissionPrompt;
    private string _workingDirectory;
    private readonly HashSet<string> _allowedPaths;

    public PermissionService(ILogger<PermissionService> logger, IPermissionPrompt permissionPrompt)
    {
        _logger = logger;
        _permissionPrompt = permissionPrompt;
        _workingDirectory = Environment.CurrentDirectory;
        _allowedPaths = new HashSet<string>();
    }

    public async Task<bool> RequestPermissionAsync(string operation, string path, string? details = null)
    {
        var fullPath = GetSafePath(path);
        
        // Check if path is within working directory
        if (!IsPathAllowed(fullPath))
        {
            _logger.LogWarning("Access denied: Path '{Path}' is outside the working directory", fullPath);
            return false;
        }

        // Request permission through the prompt interface
        var confirm = await _permissionPrompt.PromptForPermissionAsync(operation, fullPath, details);
        
        if (confirm)
        {
            _logger.LogInformation("Permission granted for {Operation} on {Path}", operation, fullPath);
        }
        else
        {
            _logger.LogWarning("Permission denied for {Operation} on {Path}", operation, fullPath);
        }
        
        return confirm;
    }

    public bool IsPathAllowed(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var workingPath = Path.GetFullPath(_workingDirectory);
            
            // Check if path is within working directory
            return fullPath.StartsWith(workingPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking path permission for {Path}", path);
            return false;
        }
    }

    public void SetWorkingDirectory(string path)
    {
        _workingDirectory = Path.GetFullPath(path);
        _logger.LogInformation("Working directory set to {Path}", _workingDirectory);
    }

    public string GetSafePath(string requestedPath)
    {
        // If path is absolute and outside working directory, reject it
        if (Path.IsPathRooted(requestedPath))
        {
            var fullPath = Path.GetFullPath(requestedPath);
            if (!IsPathAllowed(fullPath))
            {
                // Convert to relative path within working directory
                var relativePath = Path.GetFileName(requestedPath);
                return Path.Combine(_workingDirectory, relativePath);
            }
            return fullPath;
        }
        
        // Relative path - make it relative to working directory
        return Path.GetFullPath(Path.Combine(_workingDirectory, requestedPath));
    }
}