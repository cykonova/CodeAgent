using System.Text;
using System.Text.Json;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeAgent.MCP;

public class MCPClient : IMCPClient
{
    private readonly MCPOptions _options;
    private readonly ILogger<MCPClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _connectedServerUrl;
    private List<MCPTool> _cachedTools = new();

    public string Name => "MCP Client";
    public bool IsConnected => !string.IsNullOrWhiteSpace(_connectedServerUrl);

    public MCPClient(IOptions<MCPOptions> options, ILogger<MCPClient> logger, HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<bool> ConnectAsync(string serverUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Connecting to MCP server at {ServerUrl}", serverUrl);
            
            // Send initialization request
            var initRequest = new
            {
                jsonrpc = "2.0",
                method = "initialize",
                @params = new
                {
                    protocolVersion = "1.0",
                    capabilities = new
                    {
                        tools = true,
                        resources = true
                    }
                },
                id = Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(initRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{serverUrl}/rpc", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseContent);
                
                if (doc.RootElement.TryGetProperty("result", out var result))
                {
                    _connectedServerUrl = serverUrl;
                    _logger.LogInformation("Successfully connected to MCP server");
                    
                    // Cache available tools
                    await RefreshToolCacheAsync(cancellationToken);
                    
                    return true;
                }
            }
            
            _logger.LogError("Failed to connect to MCP server: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to MCP server");
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;
        
        try
        {
            _logger.LogInformation("Disconnecting from MCP server");
            _connectedServerUrl = null;
            _cachedTools.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from MCP server");
        }
    }

    public async Task<IEnumerable<MCPTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot get tools - not connected to MCP server");
            return Enumerable.Empty<MCPTool>();
        }

        if (_cachedTools.Any())
            return _cachedTools;

        await RefreshToolCacheAsync(cancellationToken);
        return _cachedTools;
    }

    public async Task<MCPToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return new MCPToolResult
            {
                Success = false,
                Error = "Not connected to MCP server"
            };
        }

        try
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments = parameters ?? new Dictionary<string, object>()
                },
                id = Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_connectedServerUrl}/rpc", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MCP tool execution failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new MCPToolResult
                {
                    Success = false,
                    Error = $"Server returned {response.StatusCode}"
                };
            }

            using var doc = JsonDocument.Parse(responseContent);
            
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var errorMessage = error.TryGetProperty("message", out var msg) 
                    ? msg.GetString() 
                    : "Unknown error";
                    
                return new MCPToolResult
                {
                    Success = false,
                    Error = errorMessage
                };
            }
            
            if (doc.RootElement.TryGetProperty("result", out var result))
            {
                return new MCPToolResult
                {
                    Success = true,
                    Data = result.GetRawText()
                };
            }
            
            return new MCPToolResult
            {
                Success = false,
                Error = "Invalid response format"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP tool {ToolName}", toolName);
            return new MCPToolResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task RefreshToolCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "tools/list",
                @params = new { },
                id = Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_connectedServerUrl}/rpc", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseContent);
                
                if (doc.RootElement.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("tools", out var tools))
                {
                    _cachedTools.Clear();
                    
                    foreach (var tool in tools.EnumerateArray())
                    {
                        var mcpTool = new MCPTool
                        {
                            Name = tool.GetProperty("name").GetString() ?? string.Empty,
                            Description = tool.TryGetProperty("description", out var desc) 
                                ? desc.GetString() ?? string.Empty 
                                : string.Empty
                        };
                        
                        if (tool.TryGetProperty("inputSchema", out var schema) &&
                            schema.TryGetProperty("properties", out var properties))
                        {
                            foreach (var prop in properties.EnumerateObject())
                            {
                                var param = new MCPParameter
                                {
                                    Name = prop.Name,
                                    Type = prop.Value.TryGetProperty("type", out var type) 
                                        ? type.GetString() ?? "string" 
                                        : "string",
                                    Description = prop.Value.TryGetProperty("description", out var paramDesc) 
                                        ? paramDesc.GetString() ?? string.Empty 
                                        : string.Empty
                                };
                                
                                if (schema.TryGetProperty("required", out var required))
                                {
                                    param.Required = required.EnumerateArray()
                                        .Any(r => r.GetString() == prop.Name);
                                }
                                
                                mcpTool.Parameters[prop.Name] = param;
                            }
                        }
                        
                        _cachedTools.Add(mcpTool);
                    }
                    
                    _logger.LogInformation("Cached {Count} MCP tools", _cachedTools.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing MCP tool cache");
        }
    }
}