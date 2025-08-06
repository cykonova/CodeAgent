using CodeAgent.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CodeAgent.Web.Hubs;

[Authorize]
public class CollaborationHub : Hub
{
    private readonly ILogger<CollaborationHub> _logger;
    private static readonly Dictionary<string, List<string>> _sessionParticipants = new();

    public CollaborationHub(ILogger<CollaborationHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinCollaboration(string sessionId, string userName)
    {
        var groupName = $"collab_{sessionId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        if (!_sessionParticipants.ContainsKey(sessionId))
            _sessionParticipants[sessionId] = new List<string>();

        _sessionParticipants[sessionId].Add(userName);

        await Clients.Group(groupName).SendAsync("UserJoined", new
        {
            sessionId,
            userName,
            participants = _sessionParticipants[sessionId],
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("User {UserName} joined collaboration session {SessionId}", userName, sessionId);
    }

    public async Task LeaveCollaboration(string sessionId, string userName)
    {
        var groupName = $"collab_{sessionId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        if (_sessionParticipants.ContainsKey(sessionId))
        {
            _sessionParticipants[sessionId].Remove(userName);
            if (_sessionParticipants[sessionId].Count == 0)
                _sessionParticipants.Remove(sessionId);
        }

        await Clients.Group(groupName).SendAsync("UserLeft", new
        {
            sessionId,
            userName,
            participants = _sessionParticipants.GetValueOrDefault(sessionId, new List<string>()),
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("User {UserName} left collaboration session {SessionId}", userName, sessionId);
    }

    public async Task SendCollaborationMessage(string sessionId, string userName, string message)
    {
        var groupName = $"collab_{sessionId}";
        await Clients.Group(groupName).SendAsync("CollaborationMessage", new
        {
            sessionId,
            userName,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task ShareCursor(string sessionId, string userName, double x, double y)
    {
        var groupName = $"collab_{sessionId}";
        await Clients.OthersInGroup(groupName).SendAsync("CursorPosition", new
        {
            sessionId,
            userName,
            x,
            y,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task ShareSelection(string sessionId, string userName, string filePath, int startLine, int endLine)
    {
        var groupName = $"collab_{sessionId}";
        await Clients.OthersInGroup(groupName).SendAsync("SelectionShared", new
        {
            sessionId,
            userName,
            filePath,
            startLine,
            endLine,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyEdit(string sessionId, string userName, string filePath, string change)
    {
        var groupName = $"collab_{sessionId}";
        await Clients.OthersInGroup(groupName).SendAsync("EditNotification", new
        {
            sessionId,
            userName,
            filePath,
            change,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task RequestControl(string sessionId, string userName)
    {
        var groupName = $"collab_{sessionId}";
        await Clients.Group(groupName).SendAsync("ControlRequested", new
        {
            sessionId,
            userName,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task GrantControl(string sessionId, string toUserName)
    {
        var groupName = $"collab_{sessionId}";
        await Clients.Group(groupName).SendAsync("ControlGranted", new
        {
            sessionId,
            toUserName,
            timestamp = DateTime.UtcNow
        });
    }
}