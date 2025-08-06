using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface ILLMProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
}