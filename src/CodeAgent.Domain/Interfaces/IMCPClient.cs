using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IMCPClient
{
    string Name { get; }
    bool IsConnected { get; }
    
    Task<bool> ConnectAsync(string serverUrl, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<MCPTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);
    Task<MCPToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}