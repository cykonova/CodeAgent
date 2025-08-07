using System.Text;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

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
            },
            new ToolDefinition
            {
                Name = "respond_to_user",
                Description = "Send a SHORT message to the user (max 2000 chars). Use for communication only, NOT for code. Use write_file for code",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["message"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The message to send to the user",
                        Required = true
                    }
                }
            },
            new ToolDefinition
            {
                Name = "execute_bash",
                Description = "Execute a bash command (security restrictions apply). Use for npm, git, and other development commands",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["command"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The bash command to execute",
                        Required = true
                    },
                    ["working_directory"] = new ParameterDefinition
                    {
                        Type = "string",
                        Description = "The working directory for the command (optional, defaults to current)",
                        Required = false
                    }
                }
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
                    
                case "respond_to_user":
                    return RespondToUser(toolCall);
                    
                case "execute_bash":
                    return await ExecuteBashAsync(toolCall, cancellationToken);
                    
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
        
        // Log what we received for debugging
        _logger.LogInformation("WriteFile called with path: {Path}, content length: {Length}", path, content.Length);
        if (content.Length < 10)
        {
            _logger.LogWarning("WriteFile received very short content: '{Content}'", content);
        }
        
        // Check if file exists and read it first if needed
        bool fileExists = await _fileSystemService.FileExistsAsync(path);
        string? existingContent = null;
        
        if (fileExists)
        {
            try
            {
                existingContent = await _fileSystemService.ReadFileAsync(path, cancellationToken);
                _logger.LogInformation("File {Path} exists with {Length} characters", path, existingContent.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read existing file {Path}", path);
            }
        }
        
        // Request permission to write the file
        var details = fileExists 
            ? $"Overwrite existing file ({existingContent?.Length ?? 0} chars) with {content.Length} characters"
            : $"Write {content.Length} characters";
            
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
            Content = fileExists 
                ? $"Successfully overwrote {path} with {content.Length} characters"
                : $"Successfully wrote {content.Length} characters to {path}"
        };
    }

    private async Task<ToolResult> ListFilesAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        try
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
                try
                {
                    var info = new FileInfo(file);
                    result.AppendLine($"  📄 {Path.GetFileName(file)} ({info.Length} bytes)");
                }
                catch
                {
                    // If we can't get file info, just show the name
                    result.AppendLine($"  📄 {Path.GetFileName(file)}");
                }
            }

            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = true,
                Content = result.ToString()
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = ex.Message
            };
        }
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

    private ToolResult RespondToUser(ToolCall toolCall)
    {
        if (!toolCall.Arguments.TryGetValue("message", out var messageObj) || messageObj == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Missing required parameter: message"
            };
        }

        var message = messageObj.ToString()!;
        
        // Check message size - prevent code being sent through respond_to_user
        const int maxResponseSize = 2000; // Reasonable size for actual messages, not code
        if (message.Length > maxResponseSize)
        {
            _logger.LogWarning("RespondToUser received oversized message: {Length} characters", message.Length);
            
            // Check if it looks like code
            var looksLikeCode = message.Contains("<!DOCTYPE") || 
                                message.Contains("function ") || 
                                message.Contains("import ") ||
                                message.Contains("class ") ||
                                message.Contains("const ") ||
                                message.Contains("var ") ||
                                message.Contains("```");
            
            if (looksLikeCode)
            {
                return new ToolResult
                {
                    ToolCallId = toolCall.Id,
                    Success = false,
                    Error = $"Message too long ({message.Length} chars, max {maxResponseSize}). " +
                           "It appears you're trying to send code through respond_to_user. " +
                           "IMPORTANT: Use 'write_file' tool to create files with code content. " +
                           "Use 'respond_to_user' only for short messages to communicate with the user."
                };
            }
            
            // Truncate if it's just a long message
            message = message.Substring(0, maxResponseSize) + "... [truncated]";
        }
        
        // This is a special tool - we just pass the message through
        // The shell will display it to the user
        return new ToolResult
        {
            ToolCallId = toolCall.Id,
            Success = true,
            Content = message,
            IsUserMessage = true // Special flag to indicate this should be shown to user
        };
    }

    private async Task<ToolResult> ExecuteBashAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (!toolCall.Arguments.TryGetValue("command", out var commandObj) || commandObj == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = "Missing required parameter: command"
            };
        }

        var command = commandObj.ToString()!;
        var workingDirectory = toolCall.Arguments.GetValueOrDefault("working_directory")?.ToString() ?? Environment.CurrentDirectory;

        // Security: Command whitelisting - only allow safe commands
        var allowedCommands = new[]
        {
            "npm", "npx", "node", "yarn", "pnpm", // Node.js tools
            "git", "gh",                           // Version control
            "dotnet",                               // .NET CLI
            "ng",                                   // Angular CLI
            "vue", "vite",                          // Vue/Vite
            "python", "pip", "pipenv", "poetry",   // Python
            "cargo", "rustc",                       // Rust
            "go",                                   // Go
            "java", "javac", "mvn", "gradle",      // Java
            "ls", "pwd", "echo", "cat", "grep",    // Basic utilities
            "mkdir", "touch", "cp", "mv",          // File operations
            "curl", "wget",                         // Network utilities
            "which", "whereis", "type"             // System info
        };

        // Extract the base command (first word)
        var baseCommand = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLower() ?? "";
        
        // Check if command is in whitelist
        if (!allowedCommands.Contains(baseCommand))
        {
            _logger.LogWarning("Attempted to execute non-whitelisted command: {Command}", baseCommand);
            
            // Request permission for non-whitelisted commands
            var details = $"Execute command: {command}";
            if (!await _permissionService.RequestPermissionAsync("Execute bash command", command, details))
            {
                return new ToolResult
                {
                    ToolCallId = toolCall.Id,
                    Success = false,
                    Error = $"Permission denied. Command '{baseCommand}' is not in the whitelist and user denied permission."
                };
            }
        }

        // Security: Prevent dangerous patterns even in whitelisted commands
        var dangerousPatterns = new[]
        {
            "rm -rf /",          // Dangerous deletion
            ":(){ :|:& };:",     // Fork bomb
            "> /dev/sda",        // Disk overwrite
            "dd if=/dev/zero",   // Disk operations
            "/etc/passwd",       // System files
            "/etc/shadow",       // System files
            "sudo",              // Privilege escalation
            "su ",               // User switching
            "chmod 777 /",       // Permission changes to root
            "eval(",             // Code evaluation
            "$(",                // Command substitution that could be dangerous
            "`",                 // Backtick command substitution
            "&&",                // Command chaining (limit to single commands)
            "||",                // Command chaining
            ";",                 // Command separator
            "|",                 // Piping (could be used maliciously)
            ">",                 // Redirection (except for specific safe cases)
            "<",                 // Input redirection
            "2>",                // Error redirection
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (command.Contains(pattern))
            {
                _logger.LogWarning("Blocked dangerous command pattern: {Pattern} in command: {Command}", pattern, command);
                return new ToolResult
                {
                    ToolCallId = toolCall.Id,
                    Success = false,
                    Error = $"Command blocked: contains dangerous pattern '{pattern}'. Please use simpler, safer commands."
                };
            }
        }

        // Security: Validate working directory
        try
        {
            var fullPath = Path.GetFullPath(workingDirectory);
            var currentPath = Path.GetFullPath(Environment.CurrentDirectory);
            
            // Ensure working directory is within or at the current project directory
            if (!fullPath.StartsWith(currentPath) && fullPath != currentPath)
            {
                return new ToolResult
                {
                    ToolCallId = toolCall.Id,
                    Success = false,
                    Error = $"Working directory must be within the current project directory"
                };
            }

            if (!Directory.Exists(fullPath))
            {
                return new ToolResult
                {
                    ToolCallId = toolCall.Id,
                    Success = false,
                    Error = $"Working directory does not exist: {fullPath}"
                };
            }
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = $"Invalid working directory: {ex.Message}"
            };
        }

        // Execute the command
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c {System.Text.Json.JsonSerializer.Serialize(command)}",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("Executing bash command: {Command} in directory: {Directory}", command, workingDirectory);
            
            process.Start();
            
            // Read output with timeout
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            // Wait for process with timeout (30 seconds default)
            var completed = process.WaitForExit(30000);
            
            if (!completed)
            {
                process.Kill();
                return new ToolResult
                {
                    ToolCallId = toolCall.Id,
                    Success = false,
                    Error = "Command timed out after 30 seconds"
                };
            }

            var output = await outputTask;
            var error = await errorTask;
            
            var result = new StringBuilder();
            if (!string.IsNullOrEmpty(output))
            {
                result.AppendLine(output);
            }
            if (!string.IsNullOrEmpty(error))
            {
                result.AppendLine($"[stderr]: {error}");
            }

            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = process.ExitCode == 0,
                Content = result.ToString(),
                Error = process.ExitCode != 0 ? $"Command exited with code {process.ExitCode}" : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bash command: {Command}", command);
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Success = false,
                Error = $"Failed to execute command: {ex.Message}"
            };
        }
    }
}