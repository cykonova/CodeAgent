namespace CodeAgent.Sandbox.MCP;

public interface IMcpServer
{
    Task<McpServerInfo> GetServerInfoAsync(CancellationToken cancellationToken = default);
    Task<McpResponse> SendRequestAsync(McpRequest request, CancellationToken cancellationToken = default);
    Task StartServerAsync(string sandboxId, McpServerConfig config, CancellationToken cancellationToken = default);
    Task StopServerAsync(string sandboxId, CancellationToken cancellationToken = default);
}