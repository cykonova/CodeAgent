using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeAgent.Domain.Providers;
using CodeAgent.Providers.Docker;
using CodeAgent.Providers.Ollama;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModelsController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModelsController> _logger;
        
        public ModelsController(
            IServiceProvider serviceProvider,
            ILogger<ModelsController> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        [HttpGet("{providerId}/list")]
        public async Task<IActionResult> ListModels(string providerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelManager = GetModelManager(providerId);
                if (modelManager == null)
                {
                    return NotFound($"Provider {providerId} does not support model management");
                }
                
                var models = await modelManager.ListModelsAsync(cancellationToken);
                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list models for provider {providerId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpGet("{providerId}/search")]
        public async Task<IActionResult> SearchModels(string providerId, [FromQuery] string q, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelManager = GetModelManager(providerId);
                if (modelManager == null)
                {
                    return NotFound($"Provider {providerId} does not support model management");
                }
                
                var models = await modelManager.SearchModelsAsync(q, cancellationToken);
                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to search models for provider {providerId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpGet("{providerId}/model/{modelId}")]
        public async Task<IActionResult> GetModelInfo(string providerId, string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelManager = GetModelManager(providerId);
                if (modelManager == null)
                {
                    return NotFound($"Provider {providerId} does not support model management");
                }
                
                var model = await modelManager.GetModelInfoAsync(modelId, cancellationToken);
                if (model == null)
                {
                    return NotFound($"Model {modelId} not found");
                }
                
                return Ok(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get model info for {modelId} from provider {providerId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpGet("{providerId}/install/{modelId}")]
        public async Task InstallModel(string providerId, string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelManager = GetModelManager(providerId);
                if (modelManager == null)
                {
                    Response.StatusCode = 404;
                    await Response.WriteAsync($"Provider {providerId} does not support model management", cancellationToken);
                    return;
                }
                
                // Set up Server-Sent Events
                Response.Headers.Add("Content-Type", "text/event-stream");
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                
                var progress = new Progress<ModelInstallProgress>(async (prog) =>
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(prog);
                    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                });
                
                var result = await modelManager.InstallModelAsync(modelId, progress, cancellationToken);
                
                // Send final result
                var finalProgress = new ModelInstallProgress
                {
                    ModelId = modelId,
                    Status = result.Success ? "completed" : "failed",
                    PercentComplete = result.Success ? 100 : 0,
                    CurrentOperation = result.Message
                };
                
                var finalJson = System.Text.Json.JsonSerializer.Serialize(finalProgress);
                await Response.WriteAsync($"data: {finalJson}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to install model {modelId} for provider {providerId}");
                var errorProgress = new ModelInstallProgress
                {
                    ModelId = modelId,
                    Status = "error",
                    PercentComplete = 0,
                    CurrentOperation = ex.Message
                };
                var errorJson = System.Text.Json.JsonSerializer.Serialize(errorProgress);
                await Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        
        [HttpDelete("{providerId}/uninstall/{modelId}")]
        public async Task<IActionResult> UninstallModel(string providerId, string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelManager = GetModelManager(providerId);
                if (modelManager == null)
                {
                    return NotFound($"Provider {providerId} does not support model management");
                }
                
                var success = await modelManager.UninstallModelAsync(modelId, cancellationToken);
                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to uninstall model" });
                }
                
                return Ok(new { success = true, message = $"Model {modelId} uninstalled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to uninstall model {modelId} for provider {providerId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpGet("{providerId}/installed/{modelId}")]
        public async Task<IActionResult> IsModelInstalled(string providerId, string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelManager = GetModelManager(providerId);
                if (modelManager == null)
                {
                    return NotFound($"Provider {providerId} does not support model management");
                }
                
                var isInstalled = await modelManager.IsModelInstalledAsync(modelId, cancellationToken);
                return Ok(isInstalled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to check if model {modelId} is installed for provider {providerId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpGet("{providerId}/current")]
        public async Task<IActionResult> GetCurrentModel(string providerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelManager = GetModelManager(providerId);
                if (modelManager == null)
                {
                    return NotFound($"Provider {providerId} does not support model management");
                }
                
                var currentModel = await modelManager.GetCurrentModelAsync(cancellationToken);
                return Ok(currentModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get current model for provider {providerId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpPost("{providerId}/current")]
        public async Task<IActionResult> SetCurrentModel(string providerId, [FromBody] SetCurrentModelRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelManager = GetModelManager(providerId);
                if (modelManager == null)
                {
                    return NotFound($"Provider {providerId} does not support model management");
                }
                
                var success = await modelManager.SetCurrentModelAsync(request.ModelId, cancellationToken);
                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to set current model" });
                }
                
                return Ok(new { success = true, message = $"Model {request.ModelId} set as current" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set current model for provider {providerId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpGet("docker/available")]
        public async Task<IActionResult> GetDockerModels(CancellationToken cancellationToken = default)
        {
            try
            {
                var dockerProvider = _serviceProvider.GetService<DockerLLMProvider>();
                if (dockerProvider == null)
                {
                    return NotFound("Docker LLM provider not configured");
                }
                
                var models = await dockerProvider.ListModelsAsync(cancellationToken);
                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Docker models");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpGet("ollama/library")]
        public async Task<IActionResult> GetOllamaLibrary(CancellationToken cancellationToken = default)
        {
            try
            {
                var ollamaManager = _serviceProvider.GetService<OllamaModelManager>();
                if (ollamaManager == null)
                {
                    return NotFound("Ollama model manager not configured");
                }
                
                // Get popular models from Ollama library
                var popularModels = new[] { "llama3.2", "mistral", "gemma2", "qwen2.5", "phi3", "llava" };
                var models = new List<ModelInfo>();
                
                foreach (var modelName in popularModels)
                {
                    var searchResults = await ollamaManager.SearchModelsAsync(modelName, cancellationToken);
                    models.AddRange(searchResults.Take(3)); // Take top 3 results for each
                }
                
                return Ok(models.Distinct());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Ollama library");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        private IModelManager? GetModelManager(string providerId)
        {
            return providerId.ToLower() switch
            {
                "ollama" => _serviceProvider.GetService<OllamaModelManager>(),
                "docker" => _serviceProvider.GetService<DockerLLMProvider>() as IModelManager,
                "docker-mcp" => _serviceProvider.GetService<DockerLLMProvider>() as IModelManager,
                _ => null
            };
        }
        
        public class SetCurrentModelRequest
        {
            public string ModelId { get; set; } = string.Empty;
        }
    }
}