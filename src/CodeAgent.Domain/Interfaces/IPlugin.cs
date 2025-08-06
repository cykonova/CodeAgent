namespace CodeAgent.Domain.Interfaces;

public interface IPlugin
{
    /// <summary>
    /// Unique identifier for the plugin
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display name of the plugin
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Initialize the plugin
    /// </summary>
    Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute the plugin functionality
    /// </summary>
    Task<PluginResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleanup resources
    /// </summary>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

public class PluginContext
{
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? WorkingDirectory { get; set; }
    public string? Input { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
}

public class PluginResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}