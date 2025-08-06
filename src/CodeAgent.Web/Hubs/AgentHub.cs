using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using CodeAgent.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CodeAgent.Web.Hubs;

[Authorize]
public class AgentHub : Hub
{
    private readonly ProviderManager _providerManager;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AgentHub> _logger;
    private static readonly Dictionary<string, string> _userConnections = new();

    public AgentHub(
        ProviderManager providerManager,
        ISessionService sessionService,
        ILogger<AgentHub> logger)
    {
        _providerManager = providerManager;
        _sessionService = sessionService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        _userConnections[userId] = Context.ConnectionId;
        
        await Clients.Caller.SendAsync("Connected", new { connectionId = Context.ConnectionId });
        await base.OnConnectedAsync();
        
        _logger.LogInformation("User {UserId} connected", userId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        _userConnections.Remove(userId);
        
        await base.OnDisconnectedAsync(exception);
        
        _logger.LogInformation("User {UserId} disconnected", userId);
    }

    public async Task SendMessage(string message, string? sessionId = null)
    {
        try
        {
            var currentProvider = _providerManager.CurrentProvider;
            if (currentProvider == null)
            {
                await Clients.Caller.SendAsync("Error", "No provider selected");
                return;
            }

            sessionId ??= Guid.NewGuid().ToString();
            
            // Send acknowledgment
            await Clients.Caller.SendAsync("MessageReceived", new { sessionId, message });

            // Process with provider
            var chatRequest = new CodeAgent.Domain.Models.ChatRequest
            {
                Messages = new List<ChatMessage> 
                { 
                    new ChatMessage { Role = "user", Content = message } 
                },
                Model = currentProvider.Name
            };

            // Stream response if provider supports it
            if (currentProvider.SupportsStreaming)
            {
                await foreach (var chunk in currentProvider.SendMessageStreamAsync(chatRequest))
                {
                    await Clients.Caller.SendAsync("StreamChunk", new { sessionId, chunk = chunk.Content });
                }
            }
            else
            {
                var response = await currentProvider.SendMessageAsync(chatRequest);
                await Clients.Caller.SendAsync("MessageResponse", new 
                { 
                    sessionId, 
                    content = response.Content,
                    timestamp = DateTime.UtcNow
                });
            }

            // Save to session
            await _sessionService.SaveMessageAsync(sessionId, new SessionMessage
            {
                Role = "user",
                Content = message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            await Clients.Caller.SendAsync("Error", "Failed to process message");
        }
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        await Clients.Caller.SendAsync("JoinedSession", sessionId);
        
        // Load session history
        var session = await _sessionService.LoadSessionAsync(sessionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("SessionHistory", session);
        }
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        await Clients.Caller.SendAsync("LeftSession", sessionId);
    }

    public async Task NotifyProgress(string operation, double percentage, string message)
    {
        await Clients.All.SendAsync("ProgressUpdate", new
        {
            operation,
            percentage,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task BroadcastStatus(AgentStatus status)
    {
        await Clients.All.SendAsync("StatusUpdate", status);
    }
}