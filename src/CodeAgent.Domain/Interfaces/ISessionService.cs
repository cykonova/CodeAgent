using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(string? name = null, CancellationToken cancellationToken = default);
    Task<Session?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> SaveSessionAsync(Session session, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInfo>> ListSessionsAsync(CancellationToken cancellationToken = default);
    Task<Session?> GetCurrentSessionAsync(CancellationToken cancellationToken = default);
    Task SetCurrentSessionAsync(Session session, CancellationToken cancellationToken = default);
    Task AddMessageAsync(string sessionId, SessionMessage message, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionMessage>> GetMessagesAsync(string sessionId, int? limit = null, CancellationToken cancellationToken = default);
    Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    
    // Additional methods for web API
    Task<Session> CreateSessionAsync(Session session, CancellationToken cancellationToken = default);
    Task<Session?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Session>> GetAllSessionsAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task SaveMessageAsync(string sessionId, SessionMessage message, CancellationToken cancellationToken = default);
}