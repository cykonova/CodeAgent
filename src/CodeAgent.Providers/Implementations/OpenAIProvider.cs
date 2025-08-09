using System.ClientModel;
using CodeAgent.Providers.Base;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace CodeAgent.Providers.Implementations;

public class OpenAIProvider : BaseProvider
{
    private OpenAIClient? _client;
    private ChatClient? _chatClient;
    
    public OpenAIProvider(ILogger<OpenAIProvider> logger) : base(logger)
    {
    }

    public override string Name => "OpenAI";
    public override string ProviderId => "openai";

    protected override Task<bool> ConnectInternalAsync(ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = configuration.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("API key is required for OpenAI provider");
                return Task.FromResult(false);
            }

            _client = new OpenAIClient(new ApiKeyCredential(apiKey));
            _chatClient = _client.GetChatClient("gpt-4o");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenAI client");
            return Task.FromResult(false);
        }
    }

    protected override Task<bool> ValidateConfigurationInternalAsync(ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
        {
            _logger.LogError("API key is required for OpenAI provider");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public override async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (_client == null || _chatClient == null)
            throw new InvalidOperationException("Provider is not connected");

        try
        {
            var messages = new List<OpenAI.Chat.ChatMessage>();
            
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Add(new SystemChatMessage(request.SystemPrompt));
            }

            foreach (var msg in request.Messages)
            {
                if (msg.Role.ToLower() == "user")
                {
                    messages.Add(new UserChatMessage(msg.Content));
                }
                else if (msg.Role.ToLower() == "assistant")
                {
                    messages.Add(new AssistantChatMessage(msg.Content));
                }
            }

            var modelName = request.Model ?? "gpt-4o";
            _chatClient = _client.GetChatClient(modelName);

            var options = new ChatCompletionOptions
            {
                Temperature = (float?)request.Temperature ?? 0.7f,
                MaxOutputTokenCount = request.MaxTokens,
                TopP = (float?)request.TopP
            };

            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var completion = response.Value;

            return new ChatResponse
            {
                Id = completion.Id,
                Model = completion.Model,
                Message = new Models.ChatMessage
                {
                    Role = "assistant",
                    Content = completion.Content?.FirstOrDefault()?.Text ?? string.Empty
                },
                Usage = completion.Usage != null ? new Usage
                {
                    PromptTokens = completion.Usage.InputTokenCount,
                    CompletionTokens = completion.Usage.OutputTokenCount,
                    TotalTokens = completion.Usage.TotalTokenCount
                } : null,
                CreatedAt = completion.CreatedAt.ToUnixTimeMilliseconds(),
                FinishReason = completion.FinishReason.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to OpenAI");
            throw;
        }
    }

    public override Task<IEnumerable<Model>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        var models = new List<Model>
        {
            new Model
            {
                Id = "gpt-4o",
                Name = "GPT-4o",
                Provider = ProviderId,
                ContextWindow = 128000,
                MaxTokens = 16384,
                SupportsTools = true,
                SupportsStreaming = true,
                SupportsVision = true,
                InputCostPer1kTokens = 0.0025m,
                OutputCostPer1kTokens = 0.01m
            },
            new Model
            {
                Id = "gpt-4o-mini",
                Name = "GPT-4o Mini",
                Provider = ProviderId,
                ContextWindow = 128000,
                MaxTokens = 16384,
                SupportsTools = true,
                SupportsStreaming = true,
                SupportsVision = true,
                InputCostPer1kTokens = 0.00015m,
                OutputCostPer1kTokens = 0.0006m
            },
            new Model
            {
                Id = "gpt-4-turbo",
                Name = "GPT-4 Turbo",
                Provider = ProviderId,
                ContextWindow = 128000,
                MaxTokens = 4096,
                SupportsTools = true,
                SupportsStreaming = true,
                SupportsVision = true,
                InputCostPer1kTokens = 0.01m,
                OutputCostPer1kTokens = 0.03m
            },
            new Model
            {
                Id = "gpt-3.5-turbo",
                Name = "GPT-3.5 Turbo",
                Provider = ProviderId,
                ContextWindow = 16385,
                MaxTokens = 4096,
                SupportsTools = true,
                SupportsStreaming = true,
                SupportsVision = false,
                InputCostPer1kTokens = 0.0005m,
                OutputCostPer1kTokens = 0.0015m
            }
        };

        return Task.FromResult<IEnumerable<Model>>(models);
    }
}