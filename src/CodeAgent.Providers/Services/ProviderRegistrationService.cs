using CodeAgent.Providers.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Providers.Services;

public class ProviderRegistrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProviderRegistry _registry;
    private readonly ILogger<ProviderRegistrationService> _logger;

    public ProviderRegistrationService(
        IServiceProvider serviceProvider,
        IProviderRegistry registry,
        ILogger<ProviderRegistrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering LLM providers...");
        
        var providers = _serviceProvider.GetServices<ILLMProvider>();
        
        foreach (var provider in providers)
        {
            _registry.RegisterProvider(provider);
            _logger.LogInformation("Registered provider: {ProviderId} ({Name})", 
                provider.ProviderId, provider.Name);
        }
        
        _logger.LogInformation("Provider registration complete. {Count} providers registered.", 
            providers.Count());
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}