using Microsoft.AspNetCore.Mvc;
using CodeAgent.Domain.Interfaces;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IDiffService _diffService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<FileController> _logger;

    public FileController(
        IFileSystemService fileSystemService,
        IDiffService diffService,
        IPermissionService permissionService,
        ILogger<FileController> logger)
    {
        _fileSystemService = fileSystemService;
        _diffService = diffService;
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpGet("browse")]
    public Task<IActionResult> BrowseDirectory([FromQuery] string path = "")
    {
        try
        {
            // Validate and sanitize path
            if (!IsValidPath(path))
            {
                return Task.FromResult<IActionResult>(BadRequest(new { error = "Invalid path" }));
            }

            var fullPath = string.IsNullOrEmpty(path) 
                ? Directory.GetCurrentDirectory() 
                : Path.GetFullPath(SanitizePath(path));
            
            if (!Directory.Exists(fullPath))
                return Task.FromResult<IActionResult>(NotFound(new { error = "Directory not found" }));
            
            var entries = new List<FileSystemEntry>();
            
            // Add parent directory if not at root
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(fullPath)))
            {
                entries.Add(new FileSystemEntry
                {
                    Name = "..",
                    Path = Path.GetDirectoryName(fullPath) ?? "",
                    IsDirectory = true
                });
            }
            
            // Get directories
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                var info = new DirectoryInfo(dir);
                if (!info.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    entries.Add(new FileSystemEntry
                    {
                        Name = info.Name,
                        Path = info.FullName,
                        IsDirectory = true,
                        Modified = info.LastWriteTime
                    });
                }
            }
            
            // Get files
            foreach (var file in Directory.GetFiles(fullPath))
            {
                var info = new FileInfo(file);
                if (!info.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    entries.Add(new FileSystemEntry
                    {
                        Name = info.Name,
                        Path = info.FullName,
                        IsDirectory = false,
                        Size = info.Length,
                        Modified = info.LastWriteTime
                    });
                }
            }
            
            return Task.FromResult<IActionResult>(Ok(new
            {
                CurrentPath = fullPath,
                Entries = entries.OrderBy(e => !e.IsDirectory).ThenBy(e => e.Name)
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing directory: {Path}", path);
            return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Failed to browse directory", details = ex.Message }));
        }
    }

    [HttpGet("read")]
    public async Task<IActionResult> ReadFile([FromQuery] string path)
    {
        try
        {
            var content = await _fileSystemService.ReadFileAsync(path);
            return Ok(new { path, content });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {Path}", path);
            return StatusCode(500, new { error = "Failed to read file", details = ex.Message });
        }
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditFile([FromBody] FileEditRequest request)
    {
        try
        {
            // Request permission
            var permission = await _permissionService.RequestPermissionAsync(
                $"Edit file: {request.Path}",
                $"The application wants to modify this file. The changes will be shown before applying.");
            
            if (!permission)
                return StatusCode(403, new { error = "Permission denied" });
            
            // Generate diff
            var originalContent = await _fileSystemService.ReadFileAsync(request.Path);
            var diff = await _diffService.GenerateDiffAsync(originalContent, request.Content);
            
            // Show diff and request confirmation
            var applyPermission = await _permissionService.RequestPermissionAsync(
                "Apply changes?",
                $"Diff:\n{diff}");
            
            if (!applyPermission)
                return Ok(new { applied = false, message = "Changes not applied" });
            
            // Apply changes
            await _fileSystemService.WriteFileAsync(request.Path, request.Content);
            
            _logger.LogInformation("File edited: {Path}", request.Path);
            return Ok(new { applied = true, diff, message = "Changes applied successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing file: {Path}", request.Path);
            return StatusCode(500, new { error = "Failed to edit file", details = ex.Message });
        }
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateFile([FromBody] FileCreateRequest request)
    {
        try
        {
            if (await _fileSystemService.FileExistsAsync(request.Path))
                return Conflict(new { error = "File already exists" });
            
            // Request permission
            var permission = await _permissionService.RequestPermissionAsync(
                $"Create file: {request.Path}",
                "The application wants to create a new file.");
            
            if (!permission)
                return StatusCode(403, new { error = "Permission denied" });
            
            await _fileSystemService.WriteFileAsync(request.Path, request.Content ?? string.Empty);
            
            _logger.LogInformation("File created: {Path}", request.Path);
            return Ok(new { message = "File created successfully", path = request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file: {Path}", request.Path);
            return StatusCode(500, new { error = "Failed to create file", details = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteFile([FromQuery] string path)
    {
        try
        {
            if (!await _fileSystemService.FileExistsAsync(path))
                return NotFound(new { error = "File not found" });
            
            // Request permission
            var permission = await _permissionService.RequestPermissionAsync(
                $"Delete file: {path}",
                "This action cannot be undone.");
            
            if (!permission)
                return StatusCode(403, new { error = "Permission denied" });
            
            await _fileSystemService.DeleteFileAsync(path);
            
            _logger.LogInformation("File deleted: {Path}", path);
            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", path);
            return StatusCode(500, new { error = "Failed to delete file", details = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchFiles([FromQuery] string pattern, [FromQuery] string directory = "")
    {
        try
        {
            var searchDir = string.IsNullOrEmpty(directory) 
                ? Directory.GetCurrentDirectory() 
                : Path.GetFullPath(directory);
            
            // Simple file search using Directory.GetFiles
            var files = await Task.Run(() => Directory.GetFiles(searchDir, pattern, SearchOption.AllDirectories));
            
            return Ok(new
            {
                Pattern = pattern,
                Directory = searchDir,
                Results = files.Select(f => new { Path = f, Name = Path.GetFileName(f) })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching files with pattern: {Pattern}", pattern);
            return StatusCode(500, new { error = "Failed to search files", details = ex.Message });
        }
    }

    // Security validation methods
    private bool IsValidPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        // Check for directory traversal attempts
        if (path.Contains("..") || path.Contains("~"))
            return false;

        // Check for invalid characters
        var invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
            return false;

        return true;
    }

    private string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Remove dangerous patterns
        var sanitized = path
            .Replace("..", "")
            .Replace("~", "")
            .Replace("\\", "/")
            .Trim();

        // Normalize slashes
        sanitized = Regex.Replace(sanitized, @"/+", "/");
        sanitized = sanitized.TrimStart('/');

        return sanitized;
    }
}

public class FileSystemEntry
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long? Size { get; set; }
    public DateTime Modified { get; set; }
}

public class FileEditRequest
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class FileCreateRequest
{
    public string Path { get; set; } = string.Empty;
    public string? Content { get; set; }
}