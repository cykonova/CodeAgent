using System.Text.Json;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly string _sessionsDirectory;
    private readonly ILogger<SessionService>? _logger;
    private Session? _currentSession;
    private readonly object _lockObject = new();

    public SessionService(string? sessionsDirectory = null, ILogger<SessionService>? logger = null)
    {
        _sessionsDirectory = sessionsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodeAgent",
            "Sessions");
        
        _logger = logger;
        
        // Ensure sessions directory exists
        Directory.CreateDirectory(_sessionsDirectory);
    }

    public Task<Session> CreateSessionAsync(string? name = null, CancellationToken cancellationToken = default)
    {
        var session = new Session
        {
            Id = GenerateSessionId(),
            Name = name ?? $"Session {DateTime.Now:yyyy-MM-dd HH:mm}",
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        
        _logger?.LogInformation("Created new session: {SessionId} - {Name}", session.Id, session.Name);
        
        return Task.FromResult(session);
    }

    public async Task<Session?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filePath = GetSessionFilePath(sessionId);
        
        if (!File.Exists(filePath))
        {
            _logger?.LogWarning("Session file not found: {SessionId}", sessionId);
            return null;
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var session = JsonSerializer.Deserialize<Session>(json, GetJsonOptions());
            
            if (session != null)
            {
                session.LastAccessedAt = DateTime.UtcNow;
                _logger?.LogInformation("Loaded session: {SessionId} - {Name}", session.Id, session.Name);
            }
            
            return session;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> SaveSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetSessionFilePath(session.Id);
            var directory = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            session.LastAccessedAt = DateTime.UtcNow;
            
            var json = JsonSerializer.Serialize(session, GetJsonOptions());
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            
            _logger?.LogInformation("Saved session: {SessionId} - {Name}", session.Id, session.Name);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving session: {SessionId}", session.Id);
            return false;
        }
    }

    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetSessionFilePath(sessionId);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger?.LogInformation("Deleted session: {SessionId}", sessionId);
                
                // Clear current session if it's the one being deleted
                if (_currentSession?.Id == sessionId)
                {
                    _currentSession = null;
                }
                
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting session: {SessionId}", sessionId);
            return Task.FromResult(false);
        }
    }

    public Task<IEnumerable<SessionInfo>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = new List<SessionInfo>();
        
        if (!Directory.Exists(_sessionsDirectory))
        {
            return Task.FromResult(sessions.AsEnumerable());
        }
        
        try
        {
            var files = Directory.GetFiles(_sessionsDirectory, "*.json", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var json = File.ReadAllText(file);
                    var session = JsonSerializer.Deserialize<Session>(json, GetJsonOptions());
                    
                    if (session != null)
                    {
                        sessions.Add(new SessionInfo
                        {
                            Id = session.Id,
                            Name = session.Name,
                            CreatedAt = session.CreatedAt,
                            LastAccessedAt = session.LastAccessedAt,
                            MessageCount = session.Messages.Count,
                            SizeInBytes = fileInfo.Length
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error reading session file: {File}", file);
                }
            }
            
            // Sort by last accessed date
            sessions = sessions.OrderByDescending(s => s.LastAccessedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error listing sessions");
        }
        
        return Task.FromResult(sessions.AsEnumerable());
    }

    public Task<Session?> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentSession);
    }

    public Task SetCurrentSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        _currentSession = session;
        _logger?.LogInformation("Set current session: {SessionId} - {Name}", session.Id, session.Name);
        return Task.CompletedTask;
    }

    public async Task AddMessageAsync(string sessionId, SessionMessage message, CancellationToken cancellationToken = default)
    {
        Session? session;
        
        // Check if it's the current session
        if (_currentSession?.Id == sessionId)
        {
            session = _currentSession;
        }
        else
        {
            session = await LoadSessionAsync(sessionId, cancellationToken);
        }
        
        if (session != null)
        {
            lock (_lockObject)
            {
                session.Messages.Add(message);
                session.LastAccessedAt = DateTime.UtcNow;
            }
            
            // Auto-save after adding message
            await SaveSessionAsync(session, cancellationToken);
            
            _logger?.LogDebug("Added message to session: {SessionId}, Role: {Role}", sessionId, message.Role);
        }
        else
        {
            _logger?.LogWarning("Cannot add message - session not found: {SessionId}", sessionId);
        }
    }

    public async Task<IEnumerable<SessionMessage>> GetMessagesAsync(string sessionId, int? limit = null, CancellationToken cancellationToken = default)
    {
        Session? session;
        
        // Check if it's the current session
        if (_currentSession?.Id == sessionId)
        {
            session = _currentSession;
        }
        else
        {
            session = await LoadSessionAsync(sessionId, cancellationToken);
        }
        
        if (session != null)
        {
            var messages = session.Messages;
            
            if (limit.HasValue && limit.Value > 0)
            {
                messages = messages.TakeLast(limit.Value).ToList();
            }
            
            return messages;
        }
        
        return Enumerable.Empty<SessionMessage>();
    }

    public async Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Session? session;
        
        // Check if it's the current session
        if (_currentSession?.Id == sessionId)
        {
            session = _currentSession;
        }
        else
        {
            session = await LoadSessionAsync(sessionId, cancellationToken);
        }
        
        if (session != null)
        {
            session.Messages.Clear();
            session.Context.Clear();
            session.LastAccessedAt = DateTime.UtcNow;
            
            await SaveSessionAsync(session, cancellationToken);
            
            _logger?.LogInformation("Cleared session: {SessionId}", sessionId);
        }
    }

    private string GenerateSessionId()
    {
        // Generate a readable session ID with timestamp
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var random = Guid.NewGuid().ToString().Substring(0, 8);
        return $"{timestamp}-{random}";
    }

    private string GetSessionFilePath(string sessionId)
    {
        // Sanitize session ID for file system
        var safeId = string.Join("_", sessionId.Split(Path.GetInvalidFileNameChars()));
        
        // Use date-based subdirectories for organization
        var datePart = sessionId.Split('-').FirstOrDefault() ?? "unknown";
        if (datePart.Length >= 8)
        {
            var year = datePart.Substring(0, 4);
            var month = datePart.Substring(4, 2);
            return Path.Combine(_sessionsDirectory, year, month, $"{safeId}.json");
        }
        
        return Path.Combine(_sessionsDirectory, $"{safeId}.json");
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    // Additional methods for web API
    public Task<Session> CreateSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        session.Id = GenerateSessionId();
        session.CreatedAt = DateTime.UtcNow;
        session.LastAccessedAt = DateTime.UtcNow;
        
        _logger?.LogInformation("Created new session from template: {SessionId} - {Name}", session.Id, session.Name);
        
        return Task.FromResult(session);
    }
    
    public Task<Session?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return LoadSessionAsync(sessionId, cancellationToken);
    }
    
    public async Task<IEnumerable<Session>> GetAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = new List<Session>();
        
        if (Directory.Exists(_sessionsDirectory))
        {
            var sessionFiles = Directory.GetFiles(_sessionsDirectory, "*.json", SearchOption.AllDirectories);
            
            foreach (var file in sessionFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var session = JsonSerializer.Deserialize<Session>(json, GetJsonOptions());
                    if (session != null)
                    {
                        sessions.Add(session);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load session from file: {FileName}", file);
                }
            }
        }
        
        return sessions;
    }
    
    public async Task<int> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await GetAllSessionsAsync(cancellationToken);
        var cutoff = DateTime.UtcNow.AddHours(-24);
        return sessions.Count(s => s.LastAccessedAt > cutoff);
    }
    
    public async Task SaveMessageAsync(string sessionId, SessionMessage message, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session != null)
        {
            session.Messages.Add(message);
            session.LastAccessedAt = DateTime.UtcNow;
            await SaveSessionAsync(session, cancellationToken);
        }
        else
        {
            _logger?.LogWarning("Cannot save message - session not found: {SessionId}", sessionId);
        }
    }
}