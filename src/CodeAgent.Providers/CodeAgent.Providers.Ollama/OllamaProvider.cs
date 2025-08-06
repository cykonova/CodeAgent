using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeAgent.Providers.Ollama;

public class OllamaProvider : ILLMProvider
{
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public string Name => "Ollama";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BaseUrl);
    public bool SupportsStreaming => true;

    public OllamaProvider(IOptions<OllamaOptions> options, ILogger<OllamaProvider> logger, HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ChatResponse
            {
                Error = "Ollama base URL is not configured",
                IsComplete = false
            };
        }

        try
        {
            var ollamaRequest = new
            {
                model = request.Model ?? _options.DefaultModel ?? "llama3.2",
                messages = request.Messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }).ToArray(),
                stream = false,
                options = new
                {
                    temperature = request.Temperature,
                    num_predict = request.MaxTokens ?? 2048
                }
            };

            var json = JsonSerializer.Serialize(ollamaRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_options.BaseUrl}/api/chat";
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ollama API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ChatResponse
                {
                    Error = $"Ollama API error: {response.StatusCode}",
                    IsComplete = false
                };
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            var messageContent = root.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            var tokensUsed = root.TryGetProperty("eval_count", out var evalElement) 
                ? evalElement.GetInt32() 
                : (int?)null;

            return new ChatResponse
            {
                Content = messageContent,
                Model = root.GetProperty("model").GetString(),
                TokensUsed = tokensUsed,
                IsComplete = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama API");
            return new ChatResponse
            {
                Error = $"Error: {ex.Message}",
                IsComplete = false
            };
        }
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            yield return "Error: Ollama base URL is not configured";
            yield break;
        }

        var ollamaRequest = new
        {
            model = request.Model ?? _options.DefaultModel ?? "llama3.2",
            messages = request.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToArray(),
            stream = true,
            options = new
            {
                temperature = request.Temperature,
                num_predict = request.MaxTokens ?? 2048
            }
        };

        var json = JsonSerializer.Serialize(ollamaRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{_options.BaseUrl}/api/chat";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        
        using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Ollama API error: {StatusCode} - {Content}", response.StatusCode, error);
            yield return $"Error: Ollama API returned {response.StatusCode}";
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string? textToYield = null;
            bool shouldBreak = false;
            
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    var text = contentElement.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        textToYield = text;
                    }
                }
                
                if (root.TryGetProperty("done", out var doneElement) && doneElement.GetBoolean())
                {
                    shouldBreak = true;
                }
            }
            catch (JsonException)
            {
                _logger.LogWarning("Failed to parse streaming response: {Line}", line);
                continue;
            }
            
            if (textToYield != null)
            {
                yield return textToYield;
            }
            
            if (shouldBreak)
            {
                break;
            }
        }
    }

    public async IAsyncEnumerable<ChatResponse> SendMessageStreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            yield return new ChatResponse { Error = "Ollama is not configured" };
            yield break;
        }

        await foreach (var chunk in StreamMessageAsync(request, cancellationToken))
        {
            yield return new ChatResponse
            {
                Content = chunk,
                Model = _options.DefaultModel,
                IsComplete = false
            };
        }
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_options.BaseUrl}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}