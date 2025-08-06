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
                },
                tools = request.Tools?.Select(t => new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = new
                        {
                            type = "object",
                            properties = t.Parameters.ToDictionary(
                                p => p.Key,
                                p => new
                                {
                                    type = p.Value.Type,
                                    description = p.Value.Description
                                }
                            ),
                            required = t.Parameters
                                .Where(p => p.Value.Required)
                                .Select(p => p.Key)
                                .ToArray()
                        }
                    }
                }).ToArray()
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
            
            var message = root.GetProperty("message");
            
            // Check if the response includes tool calls
            if (message.TryGetProperty("tool_calls", out var toolCallsElement))
            {
                var toolCalls = new List<ToolCall>();
                foreach (var toolCallElement in toolCallsElement.EnumerateArray())
                {
                    var function = toolCallElement.GetProperty("function");
                    var toolCall = new ToolCall
                    {
                        Id = toolCallElement.TryGetProperty("id", out var idElement) 
                            ? idElement.GetString() ?? Guid.NewGuid().ToString()
                            : Guid.NewGuid().ToString(),
                        Name = function.GetProperty("name").GetString() ?? string.Empty,
                        Arguments = ParseArgumentsFromJson(function.GetProperty("arguments").GetRawText())
                    };
                    toolCalls.Add(toolCall);
                }
                
                return new ChatResponse
                {
                    ToolCalls = toolCalls,
                    Model = root.GetProperty("model").GetString(),
                    IsComplete = true
                };
            }
            
            // Regular text response
            var messageContent = message.GetProperty("content").GetString() ?? string.Empty;
            var tokensUsed = root.TryGetProperty("eval_count", out var evalElement) 
                ? evalElement.GetInt32() 
                : (int?)null;

            // Check if the content looks like a tool call JSON
            if (messageContent.TrimStart().StartsWith("{") && messageContent.TrimEnd().EndsWith("}"))
            {
                try
                {
                    using var contentDoc = JsonDocument.Parse(messageContent);
                    var contentRoot = contentDoc.RootElement;
                    
                    // Check if it has the structure of a tool call
                    if (contentRoot.TryGetProperty("name", out var nameElement) &&
                        contentRoot.TryGetProperty("parameters", out var parametersElement))
                    {
                        var toolCall = new ToolCall
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = nameElement.GetString() ?? string.Empty,
                            Arguments = ParseArgumentsFromJson(parametersElement.GetRawText())
                        };
                        
                        return new ChatResponse
                        {
                            ToolCalls = new List<ToolCall> { toolCall },
                            Model = root.GetProperty("model").GetString(),
                            IsComplete = true
                        };
                    }
                }
                catch
                {
                    // Not a valid tool call JSON, treat as regular content
                }
            }

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

    private static Dictionary<string, object> ParseArgumentsFromJson(string json)
    {
        var arguments = new Dictionary<string, object>();
        
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            foreach (var property in root.EnumerateObject())
            {
                arguments[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null!,
                    JsonValueKind.Array => property.Value.GetRawText(),
                    JsonValueKind.Object => property.Value.GetRawText(),
                    _ => property.Value.GetRawText()
                };
            }
        }
        catch
        {
            // If parsing fails, return empty dictionary
        }
        
        return arguments;
    }
}