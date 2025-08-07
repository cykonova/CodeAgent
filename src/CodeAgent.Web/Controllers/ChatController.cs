using Microsoft.AspNetCore.Mvc;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using CodeAgent.Core.Services;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace CodeAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;
    private readonly ILLMProviderFactory _providerFactory;

    public ChatController(
        IChatService chatService, 
        ILogger<ChatController> logger,
        ILLMProviderFactory providerFactory)
    {
        _chatService = chatService;
        _logger = logger;
        _providerFactory = providerFactory;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Received chat message: {Message}", request.Message);

            // Validate request
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                _logger.LogWarning("Empty message received");
                return BadRequest(new { error = "Message cannot be empty" });
            }

            // Check if factory is available
            if (_providerFactory == null)
            {
                _logger.LogError("LLM Provider factory is null");
                return StatusCode(500, new { error = "LLM Provider factory not configured" });
            }

            // Get available providers for debugging
            var availableProviders = _providerFactory.GetAvailableProviders().ToList();
            _logger.LogInformation("Available providers: {Providers}", string.Join(", ", availableProviders));

            // Get the specified provider or use default
            ILLMProvider provider;
            var providerName = request.Provider ?? "openai";
            
            if (!availableProviders.Contains(providerName))
            {
                _logger.LogWarning("Provider {Provider} not available. Available: {Available}", 
                    providerName, string.Join(", ", availableProviders));
                
                // Use the first available provider as fallback
                if (availableProviders.Any())
                {
                    providerName = availableProviders.First();
                    _logger.LogInformation("Using fallback provider: {Provider}", providerName);
                }
                else
                {
                    return StatusCode(500, new { error = "No LLM providers are available" });
                }
            }

            try 
            {
                provider = _providerFactory.GetProvider(providerName);
                _logger.LogInformation("Successfully got provider: {Provider}", providerName);
            }
            catch (Exception providerEx)
            {
                _logger.LogError(providerEx, "Failed to get provider {Provider}", providerName);
                return StatusCode(500, new { error = $"Failed to initialize provider: {providerEx.Message}" });
            }

            // Check if provider is configured
            if (!provider.IsConfigured)
            {
                _logger.LogWarning("Provider {Provider} is not configured", providerName);
                return StatusCode(500, new { error = $"Provider '{providerName}' is not configured. Please check your configuration." });
            }

            // Send message and get response
            _logger.LogInformation("Sending message to provider {Provider}", providerName);
            var response = await _chatService.ProcessMessageAsync(request.Message);
            
            _logger.LogInformation("Received response from chat service");
            return Ok(new ChatResponse
            {
                Id = Guid.NewGuid().ToString(),
                Content = response.Content ?? "No response content",
                Role = "assistant",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message: {Message} | Stack: {Stack}", 
                ex.Message, ex.StackTrace);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("stream")]
    public async Task StreamMessage([FromQuery] string message, [FromQuery] string? provider = null)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        try
        {
            _logger.LogInformation("Streaming chat message: {Message}", message);

            await foreach (var chunk in _chatService.StreamResponseAsync(message, HttpContext.RequestAborted))
            {
                var data = JsonSerializer.Serialize(new { content = chunk });
                var sseMessage = $"data: {data}\n\n";
                await Response.WriteAsync(sseMessage);
                await Response.Body.FlushAsync();
            }

            // Send completion event
            await Response.WriteAsync("event: done\ndata: {}\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming chat message");
            var errorData = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    [HttpPost("reset")]
    public IActionResult ResetConversation()
    {
        _chatService.ClearHistory();
        
        // Clear session data
        HttpContext.Session.Clear();
        
        _logger.LogInformation("Conversation reset");
        return Ok(new { message = "Conversation reset successfully" });
    }

    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        var history = _chatService.GetHistory();
        
        // Store in session for persistence
        var sessionData = System.Text.Json.JsonSerializer.Serialize(history);
        HttpContext.Session.SetString("chat_history", sessionData);
        
        return Ok(history);
    }

    [HttpPost("save-session")]
    public IActionResult SaveSession([FromBody] SaveSessionRequest request)
    {
        try
        {
            // Save messages to session
            var messagesJson = System.Text.Json.JsonSerializer.Serialize(request.Messages);
            HttpContext.Session.SetString("chat_messages", messagesJson);
            
            // Save permissions to session
            if (request.Permissions != null)
            {
                var permissionsJson = System.Text.Json.JsonSerializer.Serialize(request.Permissions);
                HttpContext.Session.SetString("session_permissions", permissionsJson);
            }
            
            _logger.LogInformation("Session data saved");
            return Ok(new { message = "Session saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving session data");
            return StatusCode(500, new { error = "Failed to save session data" });
        }
    }

    [HttpGet("load-session")]
    public IActionResult LoadSession()
    {
        try
        {
            var messagesJson = HttpContext.Session.GetString("chat_messages");
            var permissionsJson = HttpContext.Session.GetString("session_permissions");
            
            var response = new LoadSessionResponse
            {
                Messages = string.IsNullOrEmpty(messagesJson) ? new List<object>() : 
                    System.Text.Json.JsonSerializer.Deserialize<List<object>>(messagesJson) ?? new List<object>(),
                Permissions = string.IsNullOrEmpty(permissionsJson) ? null : 
                    System.Text.Json.JsonSerializer.Deserialize<object>(permissionsJson)
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading session data");
            return StatusCode(500, new { error = "Failed to load session data" });
        }
    }

    [HttpGet("test")]
    public IActionResult TestConnection()
    {
        try
        {
            _logger.LogInformation("Test endpoint called");
            
            var availableProviders = _providerFactory?.GetAvailableProviders()?.ToList() ?? new List<string>();
            
            return Ok(new { 
                message = "API is working",
                availableProviders = availableProviders,
                timestamp = DateTime.UtcNow,
                chatServiceAvailable = _chatService != null,
                providerFactoryAvailable = _providerFactory != null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test endpoint failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        // Return available providers
        var providers = new[]
        {
            new { id = "openai", name = "OpenAI", models = new[] { "gpt-4", "gpt-3.5-turbo" } },
            new { id = "claude", name = "Claude", models = new[] { "claude-3-opus", "claude-3-sonnet" } },
            new { id = "ollama", name = "Ollama", models = new[] { "llama2", "codellama", "mistral" } }
        };
        
        return Ok(providers);
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; } = false;
    public string? ContextPath { get; set; }
}

public class ChatResponse
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public Usage? Usage { get; set; }
}

public class ToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public object? Result { get; set; }
}

public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class SaveSessionRequest
{
    public List<object> Messages { get; set; } = new();
    public object? Permissions { get; set; }
}

public class LoadSessionResponse
{
    public List<object> Messages { get; set; } = new();
    public object? Permissions { get; set; }
}