using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using CodeAgent.Sandbox.Services;

namespace CodeAgent.Sandbox.MCP;

public class McpServer : IMcpServer
{
    private readonly ILogger<McpServer> _logger;
    private readonly ISandboxManager _sandboxManager;
    private readonly Dictionary<string, McpServerInstance> _servers = new();

    public McpServer(ILogger<McpServer> logger, ISandboxManager sandboxManager)
    {
        _logger = logger;
        _sandboxManager = sandboxManager;
    }

    public async Task<McpServerInfo> GetServerInfoAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new McpServerInfo
        {
            Name = "CodeAgent MCP Server",
            Version = "1.0.0",
            Capabilities = new List<McpCapability>
            {
                new McpCapability
                {
                    Name = "file_operations",
                    Description = "Read, write, and manipulate files in the sandbox",
                    Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["operation"] = new { type = "string", @enum = new[] { "read", "write", "delete", "list" } },
                            ["path"] = new { type = "string" }
                        }
                    }
                },
                new McpCapability
                {
                    Name = "command_execution",
                    Description = "Execute commands in the sandbox environment",
                    Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["command"] = new { type = "string" },
                            ["workingDirectory"] = new { type = "string" }
                        }
                    }
                },
                new McpCapability
                {
                    Name = "environment_info",
                    Description = "Get information about the sandbox environment",
                    Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["info_type"] = new { type = "string", @enum = new[] { "os", "tools", "resources" } }
                        }
                    }
                }
            }
        });
    }

    public async Task<McpResponse> SendRequestAsync(McpRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.SandboxId))
        {
            return new McpResponse
            {
                Success = false,
                Error = "SandboxId is required"
            };
        }

        if (!_servers.TryGetValue(request.SandboxId, out var server))
        {
            return new McpResponse
            {
                Success = false,
                Error = $"No MCP server running for sandbox {request.SandboxId}"
            };
        }

        try
        {
            var result = request.Method switch
            {
                "file.read" => await HandleFileRead(request, cancellationToken),
                "file.write" => await HandleFileWrite(request, cancellationToken),
                "file.list" => await HandleFileList(request, cancellationToken),
                "command.execute" => await HandleCommandExecute(request, cancellationToken),
                "environment.info" => await HandleEnvironmentInfo(request, cancellationToken),
                _ => throw new NotSupportedException($"Method {request.Method} is not supported")
            };

            return new McpResponse
            {
                Success = true,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request {Method}", request.Method);
            return new McpResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task StartServerAsync(string sandboxId, McpServerConfig config, CancellationToken cancellationToken = default)
    {
        if (_servers.ContainsKey(sandboxId))
        {
            _logger.LogWarning("MCP server already running for sandbox {SandboxId}", sandboxId);
            return;
        }

        var sandbox = await _sandboxManager.GetSandboxAsync(sandboxId, cancellationToken);
        
        var serverInstance = new McpServerInstance
        {
            SandboxId = sandboxId,
            Config = config,
            StartedAt = DateTime.UtcNow
        };

        // Start MCP server process in the sandbox
        var startCommand = $"mcp-server --port {config.Port} --type {config.ServerType}";
        foreach (var env in config.Environment)
        {
            startCommand = $"{env.Key}={env.Value} {startCommand}";
        }

        var result = await _sandboxManager.ExecuteCommandAsync(sandboxId, startCommand, cancellationToken);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to start MCP server: {result.StandardError}");
        }

        _servers[sandboxId] = serverInstance;
        _logger.LogInformation("Started MCP server for sandbox {SandboxId} on port {Port}", sandboxId, config.Port);
    }

    public async Task StopServerAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        if (!_servers.TryGetValue(sandboxId, out var server))
        {
            _logger.LogWarning("No MCP server running for sandbox {SandboxId}", sandboxId);
            return;
        }

        // Stop MCP server process in the sandbox
        var result = await _sandboxManager.ExecuteCommandAsync(sandboxId, "pkill -f mcp-server", cancellationToken);
        
        _servers.Remove(sandboxId);
        _logger.LogInformation("Stopped MCP server for sandbox {SandboxId}", sandboxId);
    }

    private async Task<object> HandleFileRead(McpRequest request, CancellationToken cancellationToken)
    {
        var path = request.Parameters?["path"]?.ToString() ?? throw new ArgumentException("Path is required");
        var fullPath = SanitizePath(path);
        
        var command = $"cat {fullPath}";
        var result = await _sandboxManager.ExecuteCommandAsync(request.SandboxId!, command, cancellationToken);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to read file: {result.StandardError}");
        }

        return new { content = result.StandardOutput, path = fullPath };
    }

    private async Task<object> HandleFileWrite(McpRequest request, CancellationToken cancellationToken)
    {
        var path = request.Parameters?["path"]?.ToString() ?? throw new ArgumentException("Path is required");
        var content = request.Parameters?["content"]?.ToString() ?? "";
        var fullPath = SanitizePath(path);
        
        // Use echo with proper escaping for file write
        var escapedContent = content.Replace("'", "'\\''");
        var command = $"echo '{escapedContent}' > {fullPath}";
        var result = await _sandboxManager.ExecuteCommandAsync(request.SandboxId!, command, cancellationToken);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to write file: {result.StandardError}");
        }

        return new { path = fullPath, size = Encoding.UTF8.GetByteCount(content) };
    }

    private async Task<object> HandleFileList(McpRequest request, CancellationToken cancellationToken)
    {
        var path = request.Parameters?["path"]?.ToString() ?? "/workspace";
        var fullPath = SanitizePath(path);
        
        var command = $"ls -la {fullPath}";
        var result = await _sandboxManager.ExecuteCommandAsync(request.SandboxId!, command, cancellationToken);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to list directory: {result.StandardError}");
        }

        var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var files = lines.Skip(1).Select(line =>
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 9)
            {
                return new
                {
                    name = string.Join(" ", parts.Skip(8)),
                    type = parts[0].StartsWith('d') ? "directory" : "file",
                    size = parts[4],
                    modified = $"{parts[5]} {parts[6]} {parts[7]}"
                };
            }
            return null;
        }).Where(f => f != null);

        return new { path = fullPath, files };
    }

    private async Task<object> HandleCommandExecute(McpRequest request, CancellationToken cancellationToken)
    {
        var command = request.Parameters?["command"]?.ToString() ?? throw new ArgumentException("Command is required");
        var workingDirectory = request.Parameters?["workingDirectory"]?.ToString() ?? "/workspace";
        
        var fullCommand = $"cd {SanitizePath(workingDirectory)} && {command}";
        var result = await _sandboxManager.ExecuteCommandAsync(request.SandboxId!, fullCommand, cancellationToken);
        
        return new
        {
            exitCode = result.ExitCode,
            stdout = result.StandardOutput,
            stderr = result.StandardError,
            duration = result.Duration.TotalMilliseconds
        };
    }

    private async Task<object> HandleEnvironmentInfo(McpRequest request, CancellationToken cancellationToken)
    {
        var infoType = request.Parameters?["info_type"]?.ToString() ?? "os";
        
        var command = infoType switch
        {
            "os" => "uname -a && cat /etc/os-release",
            "tools" => "which node python dotnet docker && node --version && python --version && dotnet --version",
            "resources" => "free -h && df -h /workspace && nproc",
            _ => throw new ArgumentException($"Unknown info type: {infoType}")
        };

        var result = await _sandboxManager.ExecuteCommandAsync(request.SandboxId!, command, cancellationToken);
        
        return new
        {
            type = infoType,
            info = result.StandardOutput,
            error = result.StandardError
        };
    }

    private string SanitizePath(string path)
    {
        // Ensure path is within workspace
        if (!path.StartsWith("/workspace"))
        {
            path = Path.Combine("/workspace", path.TrimStart('/'));
        }
        
        // Remove any path traversal attempts
        path = path.Replace("..", "");
        
        return path;
    }

    private class McpServerInstance
    {
        public string SandboxId { get; set; } = string.Empty;
        public McpServerConfig Config { get; set; } = new();
        public DateTime StartedAt { get; set; }
    }
}