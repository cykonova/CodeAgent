using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CodeAgent.Domain.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeAgent.Providers.Ollama
{
    /// <summary>
    /// Model management for Ollama provider
    /// </summary>
    public class OllamaModelManager : IModelManager
    {
        private readonly OllamaOptions _options;
        private readonly ILogger<OllamaModelManager> _logger;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        
        public OllamaModelManager(
            IOptions<OllamaOptions> options,
            ILogger<OllamaModelManager> logger,
            HttpClient httpClient)
        {
            _options = options.Value;
            _logger = logger;
            _httpClient = httpClient;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            
            if (!string.IsNullOrEmpty(_options.BaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }
        }
        
        public async Task<IEnumerable<ModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<OllamaModelsResponse>(json, _jsonOptions);
                
                var models = new List<ModelInfo>();
                
                if (result?.Models != null)
                {
                    foreach (var model in result.Models)
                    {
                        models.Add(new ModelInfo
                        {
                            Id = model.Name,
                            Name = model.Name,
                            Description = $"Ollama model: {model.Name}",
                            Version = model.Digest?.Substring(0, 12),
                            Size = model.Size,
                            IsInstalled = true,
                            Provider = "Ollama",
                            LastModified = model.ModifiedAt,
                            Format = model.Details?.Format,
                            Family = model.Details?.Family,
                            Parameters = model.Details?.ParameterSize != null 
                                ? new Dictionary<string, object> { ["parameter_size"] = model.Details.ParameterSize }
                                : new Dictionary<string, object>(),
                            Capabilities = new ModelCapabilities
                            {
                                SupportsChat = true,
                                SupportsCompletion = true,
                                SupportsStreaming = true,
                                SupportsTools = DetermineToolSupport(model.Name),
                                SupportsVision = DetermineVisionSupport(model.Name),
                                ContextWindow = model.Details != null ? GetContextWindow(model.Details) : 4096,
                                MaxTokens = 4096
                            }
                        });
                    }
                }
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list Ollama models");
                return Enumerable.Empty<ModelInfo>();
            }
        }
        
        public async Task<IEnumerable<ModelInfo>> SearchModelsAsync(string query, CancellationToken cancellationToken = default)
        {
            try
            {
                // Search from Ollama library
                var response = await _httpClient.GetAsync($"https://ollama.ai/library/search?q={Uri.EscapeDataString(query)}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var searchResults = JsonSerializer.Deserialize<List<OllamaLibraryModel>>(json, _jsonOptions);
                    
                    var models = new List<ModelInfo>();
                    foreach (var result in searchResults ?? new List<OllamaLibraryModel>())
                    {
                        models.Add(new ModelInfo
                        {
                            Id = result.Name,
                            Name = result.Name,
                            Description = result.Description,
                            Tags = result.Tags ?? new List<string>(),
                            IsInstalled = false,
                            Provider = "Ollama",
                            Capabilities = new ModelCapabilities
                            {
                                SupportsChat = true,
                                SupportsStreaming = true,
                                SupportsTools = DetermineToolSupport(result.Name),
                                SupportsVision = DetermineVisionSupport(result.Name)
                            }
                        });
                    }
                    
                    return models;
                }
                
                // Fallback to local search
                var localModels = await ListModelsAsync(cancellationToken);
                return localModels.Where(m => 
                    m.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    m.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to search Ollama models for query: {query}");
                return Enumerable.Empty<ModelInfo>();
            }
        }
        
        public async Task<ModelInfo?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsync("/api/show",
                    new StringContent(JsonSerializer.Serialize(new { name = modelId }), Encoding.UTF8, "application/json"),
                    cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var model = JsonSerializer.Deserialize<OllamaModelDetails>(json, _jsonOptions);
                    
                    return new ModelInfo
                    {
                        Id = modelId,
                        Name = modelId,
                        Description = model?.Modelfile,
                        License = model?.License,
                        IsInstalled = true,
                        Provider = "Ollama",
                        Parameters = model?.Parameters ?? new Dictionary<string, object>(),
                        Capabilities = new ModelCapabilities
                        {
                            SupportsChat = true,
                            SupportsStreaming = true,
                            SupportsTools = DetermineToolSupport(modelId),
                            SupportsVision = DetermineVisionSupport(modelId)
                        }
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get info for Ollama model: {modelId}");
                return null;
            }
        }
        
        public async Task<bool> IsModelInstalledAsync(string modelId, CancellationToken cancellationToken = default)
        {
            var models = await ListModelsAsync(cancellationToken);
            return models.Any(m => m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
        }
        
        public async Task<ModelInstallResult> InstallModelAsync(
            string modelId,
            IProgress<ModelInstallProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Start the pull operation
                var pullRequest = new { name = modelId };
                var response = await _httpClient.PostAsync("/api/pull",
                    new StringContent(JsonSerializer.Serialize(pullRequest), Encoding.UTF8, "application/json"),
                    cancellationToken);
                
                response.EnsureSuccessStatusCode();
                
                // Monitor progress
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new System.IO.StreamReader(stream);
                
                string lastStatus = "";
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    
                    try
                    {
                        var status = JsonSerializer.Deserialize<OllamaPullStatus>(line, _jsonOptions);
                        if (status != null && progress != null)
                        {
                            var prog = new ModelInstallProgress
                            {
                                ModelId = modelId,
                                Status = status?.Status ?? "processing",
                                CurrentOperation = status?.Status,
                                BytesDownloaded = status?.Completed ?? 0,
                                TotalBytes = status?.Total ?? 0,
                                PercentComplete = (status?.Total ?? 0) > 0 
                                    ? (status?.Completed ?? 0) * 100.0 / (status?.Total ?? 1) 
                                    : 0
                            };
                            progress.Report(prog);
                        }
                        lastStatus = status?.Status ?? lastStatus;
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed JSON lines
                    }
                }
                
                return new ModelInstallResult
                {
                    Success = lastStatus.Contains("success", StringComparison.OrdinalIgnoreCase),
                    ModelId = modelId,
                    Message = $"Model {modelId} installed successfully",
                    InstalledAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to install Ollama model: {modelId}");
                return new ModelInstallResult
                {
                    Success = false,
                    ModelId = modelId,
                    Message = ex.Message
                };
            }
        }
        
        public async Task<bool> UninstallModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { name = modelId };
                var response = await _httpClient.DeleteAsync($"/api/delete",
                    cancellationToken);
                
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var deleteResponse = await _httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Delete, "/api/delete") { Content = content },
                    cancellationToken);
                
                return deleteResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to uninstall Ollama model: {modelId}");
                return false;
            }
        }
        
        public Task<string> GetCurrentModelAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_options.DefaultModel ?? "llama3.2");
        }
        
        public Task<bool> SetCurrentModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            _options.DefaultModel = modelId;
            return Task.FromResult(true);
        }
        
        private bool DetermineToolSupport(string modelName)
        {
            // Models known to support function calling
            var toolSupportedModels = new[] { "llama3.2", "mistral", "mixtral", "qwen2.5" };
            return toolSupportedModels.Any(m => modelName.Contains(m, StringComparison.OrdinalIgnoreCase));
        }
        
        private bool DetermineVisionSupport(string modelName)
        {
            // Models known to support vision
            var visionModels = new[] { "llava", "bakllava", "llama3.2-vision" };
            return visionModels.Any(m => modelName.Contains(m, StringComparison.OrdinalIgnoreCase));
        }
        
        private int GetContextWindow(OllamaModelDetails? details)
        {
            // Try to determine context window from model details
            if (details?.Parameters?.TryGetValue("num_ctx", out var ctx) == true)
            {
                if (int.TryParse(ctx?.ToString(), out var window))
                    return window;
            }
            return 4096; // Default
        }
        
        // Response models
        private class OllamaModelsResponse
        {
            public List<OllamaModel>? Models { get; set; }
        }
        
        private class OllamaModel
        {
            public string Name { get; set; } = string.Empty;
            public DateTime ModifiedAt { get; set; }
            public long Size { get; set; }
            public string? Digest { get; set; }
            public OllamaModelDetails? Details { get; set; }
        }
        
        private class OllamaModelDetails
        {
            public string? Format { get; set; }
            public string? Family { get; set; }
            public string? ParameterSize { get; set; }
            public string? Modelfile { get; set; }
            public string? License { get; set; }
            public Dictionary<string, object>? Parameters { get; set; }
        }
        
        private class OllamaLibraryModel
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public List<string>? Tags { get; set; }
        }
        
        private class OllamaPullStatus
        {
            public string? Status { get; set; }
            public long? Total { get; set; }
            public long? Completed { get; set; }
        }
    }
}