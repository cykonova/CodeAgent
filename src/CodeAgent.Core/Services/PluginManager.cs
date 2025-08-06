using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;

namespace CodeAgent.Core.Services;

public class PluginManager : IPluginManager, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PluginManager> _logger;
    private readonly Dictionary<string, IPlugin> _plugins = new();
    private readonly Dictionary<string, AssemblyLoadContext> _pluginContexts = new();

    public PluginManager(IServiceProvider serviceProvider, ILogger<PluginManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task LoadPluginsAsync(string pluginDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory does not exist: {Directory}", pluginDirectory);
            return;
        }

        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f).Contains("Plugin", StringComparison.OrdinalIgnoreCase));

        foreach (var pluginFile in pluginFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                await LoadPluginAsync(pluginFile, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {File}", pluginFile);
            }
        }

        _logger.LogInformation("Loaded {Count} plugins", _plugins.Count);
    }

    private async Task LoadPluginAsync(string pluginPath, CancellationToken cancellationToken)
    {
        var pluginName = Path.GetFileNameWithoutExtension(pluginPath);
        var loadContext = new PluginLoadContext(pluginPath);
        
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(pluginPath);
            
            // Find types that implement IPlugin
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var pluginType in pluginTypes)
            {
                var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                if (plugin != null)
                {
                    await plugin.InitializeAsync(_serviceProvider, cancellationToken);
                    RegisterPlugin(plugin);
                    _pluginContexts[plugin.Id] = loadContext;
                    
                    _logger.LogInformation("Loaded plugin: {PluginName} v{Version}", 
                        plugin.Name, plugin.Version);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin assembly: {Path}", pluginPath);
            loadContext.Unload();
            throw;
        }
    }

    public void RegisterPlugin(IPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        if (_plugins.ContainsKey(plugin.Id))
        {
            _logger.LogWarning("Plugin with ID {PluginId} is already registered", plugin.Id);
            return;
        }

        _plugins[plugin.Id] = plugin;
        _logger.LogDebug("Registered plugin: {PluginId}", plugin.Id);
    }

    public IReadOnlyList<IPlugin> GetPlugins()
    {
        return _plugins.Values.ToList();
    }

    public IPlugin? GetPlugin(string pluginId)
    {
        return _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
    }

    public async Task<PluginResult> ExecutePluginAsync(string pluginId, PluginContext context, CancellationToken cancellationToken = default)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin == null)
        {
            return new PluginResult
            {
                Success = false,
                Error = $"Plugin not found: {pluginId}"
            };
        }

        try
        {
            _logger.LogDebug("Executing plugin: {PluginId}", pluginId);
            var result = await plugin.ExecuteAsync(context, cancellationToken);
            
            if (result.Success)
            {
                _logger.LogDebug("Plugin {PluginId} executed successfully", pluginId);
            }
            else
            {
                _logger.LogWarning("Plugin {PluginId} execution failed: {Error}", 
                    pluginId, result.Error);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin {PluginId} threw an exception", pluginId);
            return new PluginResult
            {
                Success = false,
                Error = $"Plugin execution failed: {ex.Message}"
            };
        }
    }

    public async Task UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin))
        {
            try
            {
                await plugin.ShutdownAsync(cancellationToken);
                _plugins.Remove(pluginId);
                
                if (_pluginContexts.TryGetValue(pluginId, out var context))
                {
                    context.Unload();
                    _pluginContexts.Remove(pluginId);
                }
                
                _logger.LogInformation("Unloaded plugin: {PluginId}", pluginId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading plugin: {PluginId}", pluginId);
            }
        }
    }

    public async Task UnloadAllPluginsAsync(CancellationToken cancellationToken = default)
    {
        var pluginIds = _plugins.Keys.ToList();
        
        foreach (var pluginId in pluginIds)
        {
            await UnloadPluginAsync(pluginId, cancellationToken);
        }
    }

    public void Dispose()
    {
        UnloadAllPluginsAsync().GetAwaiter().GetResult();
    }

    private class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}