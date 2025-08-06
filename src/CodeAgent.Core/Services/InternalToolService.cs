using System.Text;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Core.Services;

public class InternalToolService : IInternalToolService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<InternalToolService> _logger;

    public InternalToolService(IFileSystemService fileSystemService, IPermissionService permissionService, ILogger<InternalToolService> logger)
    {
        _fileSystemService = fileSystemService;
        _permissionService = permissionService;
        _logger = logger;
    }

    public List<ToolDefinition> GetAvailableTools()
    {
        return new List<ToolDefinition>
        {
            new ToolDefinition
            {
                Name = "read_file",
                Description = "Read the contents of a file",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["path"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The path to the file to read",
                        Required = true
                    }
                }
            },
            new ToolDefinition
            {
                Name = "write_file",
                Description = "Write content to a file (creates or overwrites)",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["path"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The path to the file to write",
                        Required = true
                    },
                    ["content"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The content to write to the file",
                        Required = true
                    }
                }
            },
            new ToolDefinition
            {
                Name = "list_files",
                Description = "List files and directories in a given path",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["path"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The directory path to list",
                        Required = false,
                        Default = "."
                    },
                    ["pattern"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "Optional file pattern filter (e.g., '*.cs')",
                        Required = false
                    }
                }
            },
            new ToolDefinition
            {
                Name = "create_directory",
                Description = "Create a new directory",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["path"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The path of the directory to create",
                        Required = true
                    }
                }
            },
            new ToolDefinition
            {
                Name = "delete_file",
                Description = "Delete a file",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["path"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The path to the file to delete",
                        Required = true
                    }
                }
            },
            new ToolDefinition
            {
                Name = "file_exists",
                Description = "Check if a file exists",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["path"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The path to check",
                        Required = true
                    }
                }
            },
            new ToolDefinition
            {
                Name = "get_current_directory",
                Description = "Get the current working directory",
                Parameters = new Dictionary<string, ParameterDefinition>()
            }
        };
    }

    public async Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing tool: {ToolName}", toolCall.Name);
            
            switch (toolCall.Name.ToLower())
            {
                case "read_file":
                    return await ReadFileAsync(toolCall, cancellationToken);
                    
                case "write_file":
                    return await WriteFileAsync(toolCall, cancellationToken);
                    
                case "list_files":
                    return await ListFilesAsync(toolCall, cancellationToken);
                    
                case "create_directory":
                    return await CreateDirectoryAsync(toolCall, cancellationToken);
                    
                case "delete_file":
                    return await DeleteFileAsync(toolCall, cancellationToken);
                    
                case "file_exists":
                    return await FileExistsAsync(toolCall, cancellationToken);
                    
                case "get_current_directory":
                    return GetCurrentDirectory(toolCall);
                    
                default:
                    return new ToolResult
                    {
                        ToolCallId = toolCall.Id,
                        Success = false,
                        Error = $"Unknown tool: {toolCall.Name}"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolCall.Name);
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task<ToolResult> ReadFileAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (!toolCall.Arguments.TryGetValue("path", out var pathObj) || pathObj == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Missing required parameter: path"
            };
        }

        var requestedPath = pathObj.ToString()!;
        var path = _permissionService.GetSafePath(requestedPath);
        
        // Request permission to read the file
        if (!await _permissionService.RequestPermissionAsync("Read file", path))
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Permission denied"
            };
        }
        
        if (!await _fileSystemService.FileExistsAsync(path))
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = $"File not found: {path}"
            };
        }

        var content = await _fileSystemService.ReadFileAsync(path, cancellationToken);
        return new ToolResult
        {
            ToolCallId = toolCall.Id,
            Success = true,
            Content = content
        };
    }

    private async Task<ToolResult> WriteFileAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (!toolCall.Arguments.TryGetValue("path", out var pathObj) || pathObj == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Missing required parameter: path"
            };
        }

        if (!toolCall.Arguments.TryGetValue("content", out var contentObj) || contentObj == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Missing required parameter: content"
            };
        }

        var requestedPath = pathObj.ToString()!;
        var path = _permissionService.GetSafePath(requestedPath);
        var content = contentObj.ToString()!;
        
        // Request permission to write the file
        var details = $"Write {content.Length} characters";
        if (!await _permissionService.RequestPermissionAsync("Write file", path, details))
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Permission denied"
            };
        }

        await _fileSystemService.WriteFileAsync(path, content, cancellationToken);
        
        return new ToolResult
        {
            ToolCallId = toolCall.Id,
            Success = true,
            Content = $"Successfully wrote {content.Length} characters to {path}"
        };
    }

    private async Task<ToolResult> ListFilesAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var pathValue = toolCall.Arguments.GetValueOrDefault("path")?.ToString();
        var path = string.IsNullOrWhiteSpace(pathValue) ? "." : pathValue;
        var patternValue = toolCall.Arguments.GetValueOrDefault("pattern")?.ToString();
        var pattern = string.IsNullOrWhiteSpace(patternValue) ? "*" : patternValue;

        var files = await _fileSystemService.GetFilesAsync(path, pattern, false);
        var directories = await _fileSystemService.GetDirectoriesAsync(path);

        var result = new StringBuilder();
        result.AppendLine($"Contents of {path}:");
        result.AppendLine("\nDirectories:");
        foreach (var dir in directories)
        {
            result.AppendLine($"  📁 {Path.GetFileName(dir)}");
        }
        
        result.AppendLine("\nFiles:");
        foreach (var file in files)
        {
            var info = new FileInfo(file);
            result.AppendLine($"  📄 {Path.GetFileName(file)} ({info.Length} bytes)");
        }

        return new ToolResult
        {
            ToolCallId = toolCall.Id,
            Success = true,
            Content = result.ToString()
        };
    }

    private async Task<ToolResult> CreateDirectoryAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (!toolCall.Arguments.TryGetValue("path", out var pathObj) || pathObj == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Missing required parameter: path"
            };
        }

        var requestedPath = pathObj.ToString()!;
        var path = _permissionService.GetSafePath(requestedPath);
        
        // Request permission to create directory
        if (!await _permissionService.RequestPermissionAsync("Create directory", path))
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Permission denied"
            };
        }
        
        await _fileSystemService.CreateDirectoryAsync(path);
        
        return new ToolResult
        {
            ToolCallId = toolCall.Id,
            Success = true,
            Content = $"Created directory: {path}"
        };
    }

    private async Task<ToolResult> DeleteFileAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (!toolCall.Arguments.TryGetValue("path", out var pathObj) || pathObj == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Missing required parameter: path"
            };
        }

        var requestedPath = pathObj.ToString()!;
        var path = _permissionService.GetSafePath(requestedPath);
        
        // Request permission to delete file
        if (!await _permissionService.RequestPermissionAsync("Delete file", path))
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Permission denied"
            };
        }
        
        if (!await _fileSystemService.FileExistsAsync(path))
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = $"File not found: {path}"
            };
        }

        await _fileSystemService.DeleteFileAsync(path);
        
        return new ToolResult
        {
            ToolCallId = toolCall.Id,
            Success = true,
            Content = $"Deleted file: {path}"
        };
    }

    private async Task<ToolResult> FileExistsAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (!toolCall.Arguments.TryGetValue("path", out var pathObj) || pathObj == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Missing required parameter: path"
            };
        }

        var path = pathObj.ToString()!;
        var exists = await _fileSystemService.FileExistsAsync(path);
        
        return new ToolResult
        {
            ToolCallId = toolCall.Id,
            Success = true,
            Content = exists ? $"File exists: {path}" : $"File does not exist: {path}"
        };
    }

    private ToolResult GetCurrentDirectory(ToolCall toolCall)
    {
        return new ToolResult
        {
            ToolCallId = toolCall.Id,
            Success = true,
            Content = Environment.CurrentDirectory
        };
    }
}