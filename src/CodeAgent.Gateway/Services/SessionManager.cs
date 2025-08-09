using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using CodeAgent.Gateway.Gateway;

namespace CodeAgent.Gateway.Services;

public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly ConcurrentDictionary<string, Session> _sessions;
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);
    
    public SessionManager(ILogger<SessionManager> logger)
    {
        _logger = logger;
        _sessions = new ConcurrentDictionary<string, Session>();
        
        _ = Task.Run(CleanupExpiredSessions);
    }
    
    public async Task<Session> CreateSessionAsync(string sessionId, WebSocket webSocket)
    {
        var session = new Session
        {
            Id = sessionId,
            WebSocket = webSocket,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivity = DateTimeOffset.UtcNow,
            State = new ConcurrentDictionary<string, object>()
        };
        
        if (!_sessions.TryAdd(sessionId, session))
        {
            throw new InvalidOperationException($"Session {sessionId} already exists");
        }
        
        _logger.LogInformation("Session created: {SessionId}", sessionId);
        await Task.CompletedTask;
        return session;
    }
    
    public async Task<Session?> GetSessionAsync(string sessionId)
    {
        await Task.CompletedTask;
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }
    
    public async Task RemoveSessionAsync(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            if (session.WebSocket.State == WebSocketState.Open)
            {
                await session.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Session ended",
                    CancellationToken.None);
            }
            
            session.WebSocket.Dispose();
            _logger.LogInformation("Session removed: {SessionId}", sessionId);
        }
    }
    
    public async Task UpdateActivityAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastActivity = DateTimeOffset.UtcNow;
        }
        await Task.CompletedTask;
    }
    
    private async Task CleanupExpiredSessions()
    {
        var checkInterval = TimeSpan.FromMinutes(5);
        
        while (true)
        {
            try
            {
                await Task.Delay(checkInterval);
                
                var now = DateTimeOffset.UtcNow;
                var expiredSessions = _sessions
                    .Where(kvp => now - kvp.Value.LastActivity > _sessionTimeout)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var sessionId in expiredSessions)
                {
                    _logger.LogWarning("Removing expired session: {SessionId}", sessionId);
                    await RemoveSessionAsync(sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }
    }
}