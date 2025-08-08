using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CodeAgent.Domain.Models;

namespace CodeAgent.MCP
{
    /// <summary>
    /// Docker Model Context Protocol (MCP) provider for enhanced LLM context management
    /// </summary>
    public class DockerMCPProvider : IMCPProvider
    {
        private readonly ILogger<DockerMCPProvider> _logger;
        private readonly DockerMCPOptions _options;
        private readonly HttpClient _httpClient;
        
        public string Name => "Docker MCP";
        public string Version => "1.0.0";
        public bool IsConnected { get; private set; }
        
        public DockerMCPProvider(
            ILogger<DockerMCPProvider> logger,
            IOptions<DockerMCPOptions> options,
            HttpClient httpClient)
        {
            _logger = logger;
            _options = options.Value;
            _httpClient = httpClient;
        }
        
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if Docker MCP is available
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "mcp version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                await process.WaitForExitAsync(cancellationToken);
                
                IsConnected = process.ExitCode == 0;
                
                if (IsConnected)
                {
                    _logger.LogInformation("Successfully connected to Docker MCP");
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning($"Docker MCP not available: {error}");
                }
                
                return IsConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Docker MCP");
                IsConnected = false;
                return false;
            }
        }
        
        public async Task<MCPContext> GetContextAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                await ConnectAsync(cancellationToken);
            }
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"mcp context get {sessionId} --format json",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);
                
                if (process.ExitCode == 0)
                {
                    return JsonSerializer.Deserialize<MCPContext>(output) ?? new MCPContext { SessionId = sessionId };
                }
                
                return new MCPContext { SessionId = sessionId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get MCP context for session {sessionId}");
                return new MCPContext { SessionId = sessionId };
            }
        }
        
        public async Task<bool> SetContextAsync(MCPContext context, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                await ConnectAsync(cancellationToken);
            }
            
            try
            {
                var json = JsonSerializer.Serialize(context);
                var tempFile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempFile, json, cancellationToken);
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"mcp context set --file {tempFile}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                await process.WaitForExitAsync(cancellationToken);
                
                File.Delete(tempFile);
                
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set MCP context");
                return false;
            }
        }
        
        public async Task<IEnumerable<MCPTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                await ConnectAsync(cancellationToken);
            }
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "mcp tools list --format json",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);
                
                if (process.ExitCode == 0)
                {
                    return JsonSerializer.Deserialize<List<MCPTool>>(output) ?? Enumerable.Empty<MCPTool>();
                }
                
                return Enumerable.Empty<MCPTool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available MCP tools");
                return Enumerable.Empty<MCPTool>();
            }
        }
        
        public async Task<MCPToolResult> ExecuteToolAsync(
            string toolName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                await ConnectAsync(cancellationToken);
            }
            
            try
            {
                var paramsJson = JsonSerializer.Serialize(parameters);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"mcp tool execute {toolName} --params '{paramsJson}'",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);
                
                if (process.ExitCode == 0)
                {
                    return new MCPToolResult
                    {
                        Success = true,
                        Output = output,
                        ToolName = toolName
                    };
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    return new MCPToolResult
                    {
                        Success = false,
                        Error = error,
                        ToolName = toolName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to execute MCP tool {toolName}");
                return new MCPToolResult
                {
                    Success = false,
                    Error = ex.Message,
                    ToolName = toolName
                };
            }
        }
        
        public async Task<IEnumerable<MCPResource>> GetResourcesAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                await ConnectAsync(cancellationToken);
            }
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "mcp resources list --format json",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);
                
                if (process.ExitCode == 0)
                {
                    return JsonSerializer.Deserialize<List<MCPResource>>(output) ?? Enumerable.Empty<MCPResource>();
                }
                
                return Enumerable.Empty<MCPResource>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get MCP resources");
                return Enumerable.Empty<MCPResource>();
            }
        }
        
        public async Task<bool> AddResourceAsync(MCPResource resource, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                await ConnectAsync(cancellationToken);
            }
            
            try
            {
                var json = JsonSerializer.Serialize(resource);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"mcp resource add --data '{json}'",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                await process.WaitForExitAsync(cancellationToken);
                
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add MCP resource {resource.Name}");
                return false;
            }
        }
    }
    
    public interface IMCPProvider
    {
        string Name { get; }
        string Version { get; }
        bool IsConnected { get; }
        
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
        Task<MCPContext> GetContextAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<bool> SetContextAsync(MCPContext context, CancellationToken cancellationToken = default);
        Task<IEnumerable<MCPTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);
        Task<MCPToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
        Task<IEnumerable<MCPResource>> GetResourcesAsync(CancellationToken cancellationToken = default);
        Task<bool> AddResourceAsync(MCPResource resource, CancellationToken cancellationToken = default);
    }
    
    public class DockerMCPOptions
    {
        public bool Enabled { get; set; } = true;
        public string DockerHost { get; set; } = "unix:///var/run/docker.sock";
        public int MaxContextLength { get; set; } = 32000;
        public bool AutoConnect { get; set; } = true;
    }
}