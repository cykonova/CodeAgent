using System.Text.Json;
using CodeAgent.Providers.Base;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;
using Anthropic;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

namespace CodeAgent.Providers.Implementations;

public class AnthropicProvider : BaseProvider
{
    private AnthropicClient? _client;
    
    public AnthropicProvider(ILogger<AnthropicProvider> logger) : base(logger)
    {
    }

    public override string Name => "Anthropic";
    public override string ProviderId => "anthropic";

    protected override Task<bool> ConnectInternalAsync(ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = configuration.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("API key is required for Anthropic provider");
                return Task.FromResult(false);
            }

            _client = new AnthropicClient(new APIAuthentication(apiKey));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Anthropic client");
            return Task.FromResult(false);
        }
    }

    protected override Task<bool> ValidateConfigurationInternalAsync(ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
        {
            _logger.LogError("API key is required for Anthropic provider");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public override async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (_client == null)
            throw new InvalidOperationException("Provider is not connected");

        try
        {
            var messages = new List<Message>();
            
            foreach (var msg in request.Messages)
            {
                messages.Add(new Message
                {
                    Role = msg.Role == "user" ? RoleType.User : RoleType.Assistant,
                    Content = new List<ContentBase> { new TextContent { Text = msg.Content } }
                });
            }

            var messageRequest = new MessageParameters
            {
                Messages = messages,
                MaxTokens = request.MaxTokens ?? 4096,
                Model = request.Model ?? AnthropicModels.Claude35Sonnet,
                Temperature = (decimal?)request.Temperature ?? 0.7m,
                Stream = request.Stream
            };

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messageRequest.System = new List<SystemMessage> 
                { 
                    new SystemMessage(request.SystemPrompt) 
                };
            }

            var response = await _client.Messages.GetClaudeMessageAsync(messageRequest, cancellationToken);

            return new ChatResponse
            {
                Id = response.Id ?? Guid.NewGuid().ToString(),
                Model = response.Model,
                Message = new ChatMessage
                {
                    Role = "assistant",
                    Content = (response.Content?.FirstOrDefault() as TextContent)?.Text ?? string.Empty
                },
                Usage = response.Usage != null ? new Models.Usage
                {
                    PromptTokens = response.Usage.InputTokens,
                    CompletionTokens = response.Usage.OutputTokens,
                    TotalTokens = response.Usage.InputTokens + response.Usage.OutputTokens
                } : null,
                FinishReason = response.StopReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Anthropic");
            throw;
        }
    }

    public override Task<IEnumerable<Model>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        var models = new List<Model>
        {
            new Model
            {
                Id = AnthropicModels.Claude35Sonnet,
                Name = "Claude 3.5 Sonnet",
                Provider = ProviderId,
                ContextWindow = 200000,
                MaxTokens = 8192,
                SupportsTools = true,
                SupportsStreaming = true,
                SupportsVision = true,
                InputCostPer1kTokens = 0.003m,
                OutputCostPer1kTokens = 0.015m
            },
            new Model
            {
                Id = AnthropicModels.Claude35Haiku,
                Name = "Claude 3.5 Haiku",
                Provider = ProviderId,
                ContextWindow = 200000,
                MaxTokens = 8192,
                SupportsTools = true,
                SupportsStreaming = true,
                SupportsVision = true,
                InputCostPer1kTokens = 0.0008m,
                OutputCostPer1kTokens = 0.004m
            },
            new Model
            {
                Id = AnthropicModels.Claude3Opus,
                Name = "Claude 3 Opus",
                Provider = ProviderId,
                ContextWindow = 200000,
                MaxTokens = 4096,
                SupportsTools = true,
                SupportsStreaming = true,
                SupportsVision = true,
                InputCostPer1kTokens = 0.015m,
                OutputCostPer1kTokens = 0.075m
            }
        };

        return Task.FromResult<IEnumerable<Model>>(models);
    }
}