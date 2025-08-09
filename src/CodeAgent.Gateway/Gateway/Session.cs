using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace CodeAgent.Gateway.Gateway;

public class Session
{
    public required string Id { get; init; }
    public required WebSocket WebSocket { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastActivity { get; set; }
    public required ConcurrentDictionary<string, object> State { get; init; }
    
    public async Task SendAsync(string message)
    {
        if (WebSocket.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            
            LastActivity = DateTimeOffset.UtcNow;
        }
    }
}