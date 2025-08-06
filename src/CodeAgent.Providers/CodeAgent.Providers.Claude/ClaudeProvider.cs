using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeAgent.Providers.Claude;

public class ClaudeProvider : ILLMProvider
{
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public string Name => "Claude";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public ClaudeProvider(IOptions<ClaudeOptions> options, ILogger<ClaudeProvider> logger, HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new ChatResponse
            {
                Error = "Claude API key is not configured",
                IsComplete = false
            };
        }

        try
        {
            var claudeRequest = new
            {
                model = request.Model ?? _options.DefaultModel ?? "claude-3-5-sonnet-20241022",
                messages = request.Messages.Select(m => new
                {
                    role = m.Role == "system" ? "assistant" : m.Role,
                    content = m.Content
                }).ToArray(),
                max_tokens = request.MaxTokens ?? 4096,
                temperature = request.Temperature,
                stream = false
            };

            var json = JsonSerializer.Serialize(claudeRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ChatResponse
                {
                    Error = $"Claude API error: {response.StatusCode}",
                    IsComplete = false
                };
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            var messageContent = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
            var usage = root.TryGetProperty("usage", out var usageElement) 
                ? usageElement.GetProperty("output_tokens").GetInt32() + usageElement.GetProperty("input_tokens").GetInt32()
                : (int?)null;

            return new ChatResponse
            {
                Content = messageContent,
                Model = root.GetProperty("model").GetString(),
                TokensUsed = usage,
                IsComplete = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude API");
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
            yield return "Error: Claude API key is not configured";
            yield break;
        }

        var claudeRequest = new
        {
            model = request.Model ?? _options.DefaultModel ?? "claude-3-5-sonnet-20241022",
            messages = request.Messages.Select(m => new
            {
                role = m.Role == "system" ? "assistant" : m.Role,
                content = m.Content
            }).ToArray(),
            max_tokens = request.MaxTokens ?? 4096,
            temperature = request.Temperature,
            stream = true
        };

        var json = JsonSerializer.Serialize(claudeRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = content
        };
        
        using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Claude API error: {StatusCode} - {Content}", response.StatusCode, error);
            yield return $"Error: Claude API returned {response.StatusCode}";
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6);
            if (data == "[DONE]")
                break;

            string? textToYield = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "content_block_delta")
                {
                    if (root.TryGetProperty("delta", out var deltaElement) &&
                        deltaElement.TryGetProperty("text", out var textElement))
                    {
                        var text = textElement.GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            textToYield = text;
                        }
                    }
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
        }
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return false;

        try
        {
            var request = new ChatRequest
            {
                Messages = new List<ChatMessage>
                {
                    new ChatMessage("user", "Hi")
                },
                MaxTokens = 10
            };

            var response = await SendMessageAsync(request, cancellationToken);
            return response.IsComplete && string.IsNullOrEmpty(response.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Claude connection");
            return false;
        }
    }
}