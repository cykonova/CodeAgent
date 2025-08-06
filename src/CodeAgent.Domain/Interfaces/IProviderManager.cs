using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IProviderManager
{
    /// <summary>
    /// Gets the currently active provider
    /// </summary>
    ILLMProvider? CurrentProvider { get; }
    
    /// <summary>
    /// Gets the name of the currently active provider
    /// </summary>
    string? CurrentProviderName { get; }
    
    /// <summary>
    /// Registers a new provider
    /// </summary>
    void RegisterProvider(string name, ILLMProvider provider);
    
    /// <summary>
    /// Switches to a different provider
    /// </summary>
    Task<bool> SwitchProviderAsync(string name);
    
    /// <summary>
    /// Gets a list of all registered providers
    /// </summary>
    IReadOnlyList<string> GetAvailableProviders();
    
    /// <summary>
    /// Tests connectivity for a specific provider
    /// </summary>
    Task<bool> TestProviderAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets provider capabilities
    /// </summary>
    ProviderCapabilities? GetProviderCapabilities(string name);
    
    /// <summary>
    /// Loads providers from configuration
    /// </summary>
    Task LoadProvidersAsync(CancellationToken cancellationToken = default);
}