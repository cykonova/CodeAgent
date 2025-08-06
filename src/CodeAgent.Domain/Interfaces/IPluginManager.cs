namespace CodeAgent.Domain.Interfaces;

public interface IPluginManager
{
    /// <summary>
    /// Loads plugins from a directory
    /// </summary>
    Task LoadPluginsAsync(string pluginDirectory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers a plugin
    /// </summary>
    void RegisterPlugin(IPlugin plugin);
    
    /// <summary>
    /// Gets all loaded plugins
    /// </summary>
    IReadOnlyList<IPlugin> GetPlugins();
    
    /// <summary>
    /// Gets a plugin by ID
    /// </summary>
    IPlugin? GetPlugin(string pluginId);
    
    /// <summary>
    /// Executes a plugin
    /// </summary>
    Task<PluginResult> ExecutePluginAsync(string pluginId, PluginContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unloads a plugin
    /// </summary>
    Task UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unloads all plugins
    /// </summary>
    Task UnloadAllPluginsAsync(CancellationToken cancellationToken = default);
}