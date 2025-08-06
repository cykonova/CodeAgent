using System.Runtime.CompilerServices;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;

namespace CodeAgent.Core.Services;

public class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;
    private readonly List<ChatMessage> _history;

    public ChatService(ILLMProvider llmProvider)
    {
        _llmProvider = llmProvider;
        _history = new List<ChatMessage>();
    }

    public async Task<ChatResponse> ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        var userMessage = new ChatMessage("user", message);
        _history.Add(userMessage);

        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>(_history),
            Stream = false
        };

        var response = await _llmProvider.SendMessageAsync(request, cancellationToken);
        
        if (response.IsComplete && string.IsNullOrEmpty(response.Error))
        {
            _history.Add(new ChatMessage("assistant", response.Content));
        }

        return response;
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userMessage = new ChatMessage("user", message);
        _history.Add(userMessage);

        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>(_history),
            Stream = true
        };

        var fullResponse = string.Empty;
        
        await foreach (var chunk in _llmProvider.StreamMessageAsync(request, cancellationToken))
        {
            fullResponse += chunk;
            yield return chunk;
        }

        if (!string.IsNullOrEmpty(fullResponse))
        {
            _history.Add(new ChatMessage("assistant", fullResponse));
        }
    }

    public void ClearHistory()
    {
        _history.Clear();
    }

    public List<ChatMessage> GetHistory()
    {
        return new List<ChatMessage>(_history);
    }
}