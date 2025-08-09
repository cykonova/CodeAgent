using CodeAgent.Providers.Contracts;
using CodeAgent.Providers.Implementations;
using CodeAgent.Providers.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeAgent.Providers.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviders(this IServiceCollection services)
    {
        // Register the provider registry
        services.AddSingleton<IProviderRegistry, ProviderRegistry>();
        
        // Register individual providers
        services.AddTransient<ILLMProvider, AnthropicProvider>();
        services.AddTransient<ILLMProvider, OpenAIProvider>();
        services.AddTransient<ILLMProvider, OllamaProvider>();
        
        // Register providers with the registry on startup
        services.AddHostedService<ProviderRegistrationService>();
        
        return services;
    }
}