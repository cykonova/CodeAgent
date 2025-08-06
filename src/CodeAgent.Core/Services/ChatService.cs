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
        _history = new List<ChatMessage>
        {
            new ChatMessage("system", @"You are CodeAgent, a specialized coding assistant that helps developers modify and create files in their projects. 

IMPORTANT: You are NOT a chat assistant. You are a file manipulation tool. Your responses should ALWAYS be the complete file content that results from the requested changes.

When asked to modify a file:
1. Apply the requested changes to the provided content
2. Return ONLY the complete modified file content
3. Do NOT add explanations, comments, or markdown formatting
4. Do NOT wrap code in markdown code blocks (```)
5. Preserve the original file's formatting and structure where not explicitly changed

When asked to create a new file:
1. Generate the complete file content based on the requirements
2. Return ONLY the file content
3. Do NOT add explanations or markdown formatting

Your output will be directly written to files, so it must be valid, executable code without any chat-style responses or formatting.")
        };
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