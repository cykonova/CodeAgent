using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IChatService
{
    Task<ChatResponse> ProcessMessageAsync(string message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamResponseAsync(string message, CancellationToken cancellationToken = default);
    void ClearHistory();
    List<ChatMessage> GetHistory();
}