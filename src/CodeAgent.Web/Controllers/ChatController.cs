using Microsoft.AspNetCore.Mvc;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using System.Runtime.CompilerServices;

namespace CodeAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("message")]
    public async IAsyncEnumerable<string> SendMessage([FromBody] ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received chat message: {Message}", request.Message);

        await foreach (var chunk in _chatService.StreamResponseAsync(request.Message, cancellationToken))
        {
            yield return chunk;
        }
    }

    [HttpPost("reset")]
    public IActionResult ResetConversation()
    {
        _chatService.ClearHistory();
        _logger.LogInformation("Conversation reset");
        return Ok(new { message = "Conversation reset successfully" });
    }

    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        var history = _chatService.GetHistory();
        return Ok(history);
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Context { get; set; }
}