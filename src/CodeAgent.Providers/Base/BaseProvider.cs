using CodeAgent.Providers.Contracts;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Providers.Base;

public abstract class BaseProvider : ILLMProvider
{
    protected readonly ILogger _logger;
    protected ProviderConfiguration? _configuration;

    protected BaseProvider(ILogger logger)
    {
        _logger = logger;
    }

    public abstract string Name { get; }
    public abstract string ProviderId { get; }
    public virtual bool IsConnected { get; protected set; }

    public virtual async Task<bool> ConnectAsync(ProviderConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            var isValid = await ValidateConfigurationAsync(configuration, cancellationToken);
            if (!isValid)
            {
                _logger.LogError("Invalid configuration for provider {ProviderId}", ProviderId);
                return false;
            }

            _configuration = configuration;
            IsConnected = await ConnectInternalAsync(configuration, cancellationToken);
            
            if (IsConnected)
            {
                _logger.LogInformation("Provider {ProviderId} connected successfully", ProviderId);
            }
            else
            {
                _logger.LogWarning("Failed to connect provider {ProviderId}", ProviderId);
            }

            return IsConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting provider {ProviderId}", ProviderId);
            IsConnected = false;
            return false;
        }
    }

    public virtual Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        _configuration = null;
        _logger.LogInformation("Provider {ProviderId} disconnected", ProviderId);
        return Task.CompletedTask;
    }

    public abstract Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<Model>> GetModelsAsync(CancellationToken cancellationToken = default);
    
    public virtual Task<bool> ValidateConfigurationAsync(ProviderConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            return Task.FromResult(false);

        if (string.IsNullOrWhiteSpace(configuration.ProviderId))
            return Task.FromResult(false);

        return ValidateConfigurationInternalAsync(configuration, cancellationToken);
    }

    protected abstract Task<bool> ConnectInternalAsync(ProviderConfiguration configuration, CancellationToken cancellationToken);
    protected abstract Task<bool> ValidateConfigurationInternalAsync(ProviderConfiguration configuration, CancellationToken cancellationToken);
}