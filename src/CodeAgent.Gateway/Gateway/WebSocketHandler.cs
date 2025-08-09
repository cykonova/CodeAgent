using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using CodeAgent.Gateway.Services;

namespace CodeAgent.Gateway.Gateway;

public class WebSocketHandler
{
    private readonly ILogger<WebSocketHandler> _logger;
    private readonly MessageRouter _messageRouter;
    private readonly SessionManager _sessionManager;
    
    public WebSocketHandler(
        ILogger<WebSocketHandler> logger,
        MessageRouter messageRouter,
        SessionManager sessionManager)
    {
        _logger = logger;
        _messageRouter = messageRouter;
        _sessionManager = sessionManager;
    }
    
    public async Task HandleAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = await _sessionManager.CreateSessionAsync(sessionId, webSocket);
        
        _logger.LogInformation("WebSocket connection established. SessionId: {SessionId}", sessionId);
        
        try
        {
            await StartHeartbeatAsync(webSocket, cancellationToken);
            await ProcessMessagesAsync(session, webSocket, cancellationToken);
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "WebSocket error for session {SessionId}", sessionId);
        }
        finally
        {
            await _sessionManager.RemoveSessionAsync(sessionId);
            _logger.LogInformation("WebSocket connection closed. SessionId: {SessionId}", sessionId);
        }
    }
    
    private async Task ProcessMessagesAsync(Session session, WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);
        
        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer.Array!, 0, result.Count);
                    await _messageRouter.RouteMessageAsync(session, message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        cancellationToken);
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
    
    private async Task StartHeartbeatAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            var pingInterval = TimeSpan.FromSeconds(30);
            var pingMessage = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type = "ping" }));
            
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(pingMessage),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                    
                    await Task.Delay(pingInterval, cancellationToken);
                }
                catch
                {
                    break;
                }
            }
        }, cancellationToken);
    }
}