using CodeAgent.Providers.Models;

namespace CodeAgent.Providers.Contracts;

public interface ILLMProvider
{
    string Name { get; }
    string ProviderId { get; }
    bool IsConnected { get; }
    
    Task<bool> ConnectAsync(ProviderConfiguration configuration, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<Model>> GetModelsAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(ProviderConfiguration configuration, CancellationToken cancellationToken = default);
}