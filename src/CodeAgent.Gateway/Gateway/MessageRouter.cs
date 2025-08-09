using System.Text.Json;
using System.Threading.Channels;

namespace CodeAgent.Gateway.Gateway;

public class MessageRouter
{
    private readonly ILogger<MessageRouter> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<RoutedMessage> _messageChannel;
    
    public MessageRouter(ILogger<MessageRouter> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageChannel = Channel.CreateUnbounded<RoutedMessage>();
        
        _ = Task.Run(ProcessQueuedMessages);
    }
    
    public async Task RouteMessageAsync(Session session, string rawMessage)
    {
        try
        {
            var message = JsonSerializer.Deserialize<MessageEnvelope>(rawMessage);
            if (message == null)
            {
                await SendErrorAsync(session, "Invalid message format");
                return;
            }
            
            _logger.LogDebug("Routing message type: {Type} for session: {SessionId}", 
                message.Type, session.Id);
            
            var routedMessage = new RoutedMessage
            {
                Session = session,
                Envelope = message,
                Timestamp = DateTimeOffset.UtcNow
            };
            
            await _messageChannel.Writer.WriteAsync(routedMessage);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse message from session {SessionId}", session.Id);
            await SendErrorAsync(session, "Message parsing failed");
        }
    }
    
    private async Task ProcessQueuedMessages()
    {
        await foreach (var routedMessage in _messageChannel.Reader.ReadAllAsync())
        {
            try
            {
                await HandleMessageAsync(routedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message type: {Type}", 
                    routedMessage.Envelope.Type);
                await SendErrorAsync(routedMessage.Session, 
                    $"Failed to process message: {ex.Message}");
            }
        }
    }
    
    private async Task HandleMessageAsync(RoutedMessage message)
    {
        var response = message.Envelope.Type switch
        {
            "auth" => await HandleAuthAsync(message),
            "chat" => await HandleChatAsync(message),
            "command" => await HandleCommandAsync(message),
            "ping" => new { type = "pong", timestamp = DateTimeOffset.UtcNow },
            _ => new { type = "error", message = $"Unknown message type: {message.Envelope.Type}" }
        };
        
        await SendResponseAsync(message.Session, response);
    }
    
    private async Task<object> HandleAuthAsync(RoutedMessage message)
    {
        await Task.Delay(1);
        return new 
        { 
            type = "auth_response", 
            success = true,
            sessionId = message.Session.Id 
        };
    }
    
    private async Task<object> HandleChatAsync(RoutedMessage message)
    {
        await Task.Delay(1);
        return new 
        { 
            type = "chat_response",
            message = "Chat functionality not yet implemented"
        };
    }
    
    private async Task<object> HandleCommandAsync(RoutedMessage message)
    {
        await Task.Delay(1);
        return new 
        { 
            type = "command_response",
            message = "Command processing not yet implemented"
        };
    }
    
    private async Task SendResponseAsync(Session session, object response)
    {
        var json = JsonSerializer.Serialize(response);
        await session.SendAsync(json);
    }
    
    private async Task SendErrorAsync(Session session, string error)
    {
        await SendResponseAsync(session, new { type = "error", message = error });
    }
}

public class RoutedMessage
{
    public required Session Session { get; init; }
    public required MessageEnvelope Envelope { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

public class MessageEnvelope
{
    public required string Type { get; init; }
    public JsonElement? Payload { get; init; }
    public string? CorrelationId { get; init; }
}