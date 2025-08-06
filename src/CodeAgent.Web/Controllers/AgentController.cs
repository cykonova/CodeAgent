using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using CodeAgent.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly ProviderManager _providerManager;
    private readonly ContextManager _contextManager;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        ProviderManager providerManager,
        ContextManager contextManager,
        ISessionService sessionService,
        ILogger<AgentController> logger)
    {
        _providerManager = providerManager;
        _contextManager = contextManager;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = new AgentStatus
        {
            IsOnline = true,
            CurrentProvider = _providerManager.CurrentProviderName,
            AvailableProviders = _providerManager.GetAvailableProviders().ToList(),
            ActiveSessions = await _sessionService.GetActiveSessionsAsync(),
            CurrentContext = _contextManager.GetCurrentContext()
        };

        return Ok(status);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> SendMessage([FromBody] CodeAgent.Web.Models.ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message cannot be empty");

        try
        {
            var currentProvider = _providerManager.CurrentProvider;
            if (currentProvider == null)
                return BadRequest("No provider selected");

            var chatRequest = new CodeAgent.Domain.Models.ChatRequest
            {
                Messages = new List<ChatMessage> 
                { 
                    new ChatMessage { Role = "user", Content = request.Message } 
                },
                Model = currentProvider.Name
            };

            var response = await currentProvider.SendMessageAsync(chatRequest);
            
            // Save to session
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                await _sessionService.SaveMessageAsync(request.SessionId, new SessionMessage
                {
                    Role = "user",
                    Content = request.Message,
                    Timestamp = DateTime.UtcNow
                });
                await _sessionService.SaveMessageAsync(request.SessionId, new SessionMessage
                {
                    Role = "assistant",
                    Content = response.Content,
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new CodeAgent.Web.Models.ChatResponse
            {
                Content = response.Content,
                SessionId = request.SessionId ?? "",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, "An error occurred processing your message");
        }
    }

    [HttpPost("context/build")]
    public async Task<IActionResult> BuildContext([FromBody] BuildContextRequest request)
    {
        try
        {
            var context = await _contextManager.BuildContextAsync(request.ProjectPath);
            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building context for {Path}", request.ProjectPath);
            return StatusCode(500, "Failed to build context");
        }
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = _providerManager.GetAvailableProviders()
            .Select(name => new
            {
                Name = name,
                IsActive = name == _providerManager.CurrentProviderName,
                Capabilities = _providerManager.GetProviderCapabilities(name)
            });

        return Ok(providers);
    }

    [HttpPost("providers/switch")]
    public async Task<IActionResult> SwitchProvider([FromBody] SwitchProviderRequest request)
    {
        var success = await _providerManager.SwitchProviderAsync(request.ProviderName);
        if (success)
        {
            return Ok(new { message = $"Switched to {request.ProviderName}" });
        }
        
        return BadRequest($"Failed to switch to {request.ProviderName}");
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var sessions = await _sessionService.GetActiveSessionsAsync();
        return Ok(sessions);
    }

    [HttpGet("sessions/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        var session = await _sessionService.LoadSessionAsync(sessionId);
        if (session == null)
            return NotFound();

        return Ok(session);
    }

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        await _sessionService.DeleteSessionAsync(sessionId);
        return NoContent();
    }
}