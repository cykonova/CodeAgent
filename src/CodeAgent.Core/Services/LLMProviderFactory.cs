using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodeAgent.Core.Services;

public interface ILLMProviderFactory
{
    ILLMProvider GetProvider(string providerName);
    IEnumerable<string> GetAvailableProviders();
}

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _providerTypes;

    public LLMProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _providerTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
    }

    public void RegisterProvider<T>(string name) where T : ILLMProvider
    {
        _providerTypes[name] = typeof(T);
    }

    public ILLMProvider GetProvider(string providerName)
    {
        if (!_providerTypes.TryGetValue(providerName, out var providerType))
        {
            throw new ArgumentException($"Provider '{providerName}' is not registered.");
        }

        return (ILLMProvider)_serviceProvider.GetRequiredService(providerType);
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        return _providerTypes.Keys;
    }
}