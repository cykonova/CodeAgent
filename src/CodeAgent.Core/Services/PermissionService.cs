using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Core.Services;

public class PermissionService : IPermissionService
{
    private readonly ILogger<PermissionService> _logger;
    private readonly IPermissionPrompt _permissionPrompt;
    private string _workingDirectory;
    private string? _projectDirectory;
    private readonly Dictionary<string, HashSet<string>> _allowedOperations;
    private readonly string _userProfilePath;

    public PermissionService(ILogger<PermissionService> logger, IPermissionPrompt permissionPrompt)
    {
        _logger = logger;
        _permissionPrompt = permissionPrompt;
        _workingDirectory = Environment.CurrentDirectory;
        _allowedOperations = new Dictionary<string, HashSet<string>>();
        _userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public async Task<bool> RequestPermissionAsync(string operation, string path, string? details = null)
    {
        var fullPath = GetSafePath(path);
        
        // Safety check: Deny operations outside user profile (with exception for project dir)
        if (IsOutsideUserProfile(fullPath) && !IsPathWithinDirectory(fullPath, _projectDirectory))
        {
            _logger.LogError("SECURITY: Operation '{Operation}' denied - path '{Path}' is outside user profile", operation, fullPath);
            return false;
        }
        
        // Check if this operation is already allowed for the project directory
        var projectDir = _projectDirectory ?? _workingDirectory;
        if (_allowedOperations.ContainsKey(operation) && 
            _allowedOperations[operation].Contains(projectDir) &&
            IsPathWithinDirectory(fullPath, projectDir))
        {
            _logger.LogInformation("Permission auto-granted for {Operation} on {Path} (previously allowed for project)", operation, fullPath);
            return true;
        }
        
        // Check if path is within allowed directories
        if (!IsPathAllowed(fullPath))
        {
            _logger.LogWarning("Access denied: Path '{Path}' is outside allowed directories", fullPath);
            return false;
        }

        // Request permission through the prompt interface
        var result = await _permissionPrompt.PromptForPermissionAsync(operation, fullPath, projectDir, details);
        
        switch (result)
        {
            case PermissionResult.Allowed:
                _logger.LogInformation("Permission granted for {Operation} on {Path}", operation, fullPath);
                return true;
                
            case PermissionResult.AllowedForAll:
                // Add this operation to the allowed list for the project directory
                if (!_allowedOperations.ContainsKey(operation))
                    _allowedOperations[operation] = new HashSet<string>();
                _allowedOperations[operation].Add(projectDir);
                
                _logger.LogInformation("Permission granted for {Operation} on {Path} and all future {Operation} operations in {ProjectDir}", 
                    operation, fullPath, operation, projectDir);
                return true;
                
            case PermissionResult.Denied:
            default:
                _logger.LogWarning("Permission denied for {Operation} on {Path}", operation, fullPath);
                return false;
        }
    }

    public bool IsPathAllowed(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var workingPath = Path.GetFullPath(_workingDirectory);
            var projectPath = _projectDirectory != null ? Path.GetFullPath(_projectDirectory) : null;
            
            // Check if path is within working directory or project directory
            var withinWorking = fullPath.StartsWith(workingPath, StringComparison.OrdinalIgnoreCase);
            var withinProject = projectPath != null && fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase);
            
            return withinWorking || withinProject;
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

    public void SetProjectDirectory(string path)
    {
        _projectDirectory = Path.GetFullPath(path);
        _logger.LogInformation("Project directory set to {Path}", _projectDirectory);
    }

    public bool IsOutsideUserProfile(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var userProfilePath = Path.GetFullPath(_userProfilePath);
            
            return !fullPath.StartsWith(userProfilePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if path is outside user profile: {Path}", path);
            return true; // Default to safe (deny)
        }
    }

    private bool IsPathWithinDirectory(string path, string? directory)
    {
        if (string.IsNullOrEmpty(directory))
            return false;
            
        try
        {
            var fullPath = Path.GetFullPath(path);
            var directoryPath = Path.GetFullPath(directory);
            
            return fullPath.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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