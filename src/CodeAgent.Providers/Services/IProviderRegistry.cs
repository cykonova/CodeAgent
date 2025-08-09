using CodeAgent.Providers.Contracts;
using CodeAgent.Providers.Models;

namespace CodeAgent.Providers.Services;

public interface IProviderRegistry
{
    void RegisterProvider(ILLMProvider provider);
    ILLMProvider? GetProvider(string providerId);
    IEnumerable<ILLMProvider> GetAllProviders();
    Task<ILLMProvider?> GetConnectedProviderAsync(string providerId, CancellationToken cancellationToken = default);
    Task<bool> ConnectProviderAsync(string providerId, ProviderConfiguration configuration, CancellationToken cancellationToken = default);
    Task DisconnectProviderAsync(string providerId, CancellationToken cancellationToken = default);
    bool IsProviderRegistered(string providerId);
    bool IsProviderConnected(string providerId);
}