using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeAgent.Providers.Base;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Providers.Implementations;

public class OllamaProvider : BaseProvider
{
    private HttpClient? _httpClient;
    private string _baseUrl = "http://localhost:11434";
    
    public OllamaProvider(ILogger<OllamaProvider> logger) : base(logger)
    {
    }

    public override string Name => "Ollama";
    public override string ProviderId => "ollama";

    protected override Task<bool> ConnectInternalAsync(ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        try
        {
            _baseUrl = configuration.Endpoint ?? "http://localhost:11434";
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds ?? 120)
            };
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Ollama client");
            return Task.FromResult(false);
        }
    }

    protected override async Task<bool> ValidateConfigurationInternalAsync(ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        try
        {
            var testUrl = configuration.Endpoint ?? "http://localhost:11434";
            using var testClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            var response = await testClient.GetAsync($"{testUrl}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            _logger.LogWarning("Could not connect to Ollama at {Endpoint}", configuration.Endpoint ?? "http://localhost:11434");
            return false;
        }
    }

    public override async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (_httpClient == null)
            throw new InvalidOperationException("Provider is not connected");

        try
        {
            var ollamaRequest = new OllamaGenerateRequest
            {
                Model = request.Model ?? "codellama",
                Messages = request.Messages.Select(m => new OllamaMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList(),
                Stream = false,
                Options = new OllamaOptions
                {
                    Temperature = request.Temperature ?? 0.7,
                    NumPredict = request.MaxTokens,
                    TopP = request.TopP
                }
            };

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                ollamaRequest.System = request.SystemPrompt;
            }

            var json = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson);

            if (ollamaResponse == null)
                throw new InvalidOperationException("Invalid response from Ollama");

            return new ChatResponse
            {
                Id = Guid.NewGuid().ToString(),
                Model = ollamaResponse.Model,
                Message = new ChatMessage
                {
                    Role = "assistant",
                    Content = ollamaResponse.Message?.Content ?? string.Empty
                },
                Usage = new Usage
                {
                    PromptTokens = ollamaResponse.PromptEvalCount ?? 0,
                    CompletionTokens = ollamaResponse.EvalCount ?? 0,
                    TotalTokens = (ollamaResponse.PromptEvalCount ?? 0) + (ollamaResponse.EvalCount ?? 0)
                },
                CreatedAt = ollamaResponse.CreatedAt != null ? 
                    ((DateTimeOffset)DateTime.Parse(ollamaResponse.CreatedAt)).ToUnixTimeMilliseconds() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Ollama");
            throw;
        }
    }

    public override async Task<IEnumerable<Model>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (_httpClient == null)
            return Enumerable.Empty<Model>();

        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelList = JsonSerializer.Deserialize<OllamaModelList>(json);

            if (modelList?.Models == null)
                return Enumerable.Empty<Model>();

            return modelList.Models.Select(m => new Model
            {
                Id = m.Name,
                Name = m.Name,
                Provider = ProviderId,
                ContextWindow = 4096,
                MaxTokens = 2048,
                SupportsTools = false,
                SupportsStreaming = true,
                SupportsVision = m.Name.Contains("llava") || m.Name.Contains("vision"),
                Capabilities = new Dictionary<string, object>
                {
                    ["size"] = m.Size,
                    ["modified"] = m.ModifiedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting models from Ollama");
            return Enumerable.Empty<Model>();
        }
    }

    public override Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _httpClient?.Dispose();
        _httpClient = null;
        return base.DisconnectAsync(cancellationToken);
    }
}

internal class OllamaGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = new();
    
    [JsonPropertyName("system")]
    public string? System { get; set; }
    
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
    
    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }
}

internal class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class OllamaOptions
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
    
    [JsonPropertyName("num_predict")]
    public int? NumPredict { get; set; }
    
    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }
}

internal class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
    
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }
    
    [JsonPropertyName("done")]
    public bool Done { get; set; }
    
    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; set; }
    
    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }
}

internal class OllamaModelList
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo>? Models { get; set; }
}

internal class OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
    
    [JsonPropertyName("modified_at")]
    public string ModifiedAt { get; set; } = string.Empty;
}