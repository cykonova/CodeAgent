using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodeAgent.Domain.Providers
{
    /// <summary>
    /// Interface for LLM providers that support model management
    /// </summary>
    public interface IModelManager
    {
        /// <summary>
        /// Lists all available models from the provider
        /// </summary>
        Task<IEnumerable<ModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Searches for models matching the query
        /// </summary>
        Task<IEnumerable<ModelInfo>> SearchModelsAsync(string query, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets detailed information about a specific model
        /// </summary>
        Task<ModelInfo?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if a model is installed/available locally
        /// </summary>
        Task<bool> IsModelInstalledAsync(string modelId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Installs or downloads a model
        /// </summary>
        Task<ModelInstallResult> InstallModelAsync(string modelId, IProgress<ModelInstallProgress>? progress = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Uninstalls or removes a model
        /// </summary>
        Task<bool> UninstallModelAsync(string modelId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the currently selected/active model
        /// </summary>
        Task<string> GetCurrentModelAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sets the current model to use
        /// </summary>
        Task<bool> SetCurrentModelAsync(string modelId, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Information about an LLM model
    /// </summary>
    public class ModelInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Version { get; set; }
        public long? Size { get; set; }
        public string? Format { get; set; }
        public string? Family { get; set; }
        public bool IsInstalled { get; set; }
        public DateTime? LastModified { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        // Additional metadata
        public string? License { get; set; }
        public string? Provider { get; set; }
        public ModelCapabilities? Capabilities { get; set; }
    }
    
    /// <summary>
    /// Model capabilities and features
    /// </summary>
    public class ModelCapabilities
    {
        public bool SupportsChat { get; set; } = true;
        public bool SupportsCompletion { get; set; } = true;
        public bool SupportsEmbeddings { get; set; }
        public bool SupportsVision { get; set; }
        public bool SupportsTools { get; set; }
        public bool SupportsStreaming { get; set; } = true;
        public int MaxTokens { get; set; }
        public int ContextWindow { get; set; }
        public List<string> SupportedLanguages { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Progress information for model installation
    /// </summary>
    public class ModelInstallProgress
    {
        public string ModelId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double PercentComplete { get; set; }
        public long? BytesDownloaded { get; set; }
        public long? TotalBytes { get; set; }
        public string? CurrentOperation { get; set; }
    }
    
    /// <summary>
    /// Result of a model installation
    /// </summary>
    public class ModelInstallResult
    {
        public bool Success { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? InstallPath { get; set; }
        public DateTime? InstalledAt { get; set; }
    }
}