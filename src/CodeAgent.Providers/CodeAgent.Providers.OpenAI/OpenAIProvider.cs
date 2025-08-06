using System.ClientModel;
using System.Runtime.CompilerServices;
using CodeAgent.Domain.Configuration;
using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using DomainChatMessage = CodeAgent.Domain.Models.ChatMessage;
using DomainChatResponse = CodeAgent.Domain.Models.ChatResponse;
using DomainChatRequest = CodeAgent.Domain.Models.ChatRequest;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;

namespace CodeAgent.Providers.OpenAI;

public class OpenAIProvider : ILLMProvider
{
    private readonly IConfiguration _configuration;
    private OpenAIClient? _client;
    private ChatClient? _chatClient;
    private LLMProviderSettings? _settings;

    public string Name => "OpenAI";

    public bool IsConfigured => !string.IsNullOrEmpty(_settings?.ApiKey);

    public OpenAIProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        Initialize();
    }

    private void Initialize()
    {
        _settings = _configuration.GetSection("LLMProvider").Get<LLMProviderSettings>() ?? new LLMProviderSettings();
        
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _client = new OpenAIClient(_settings.ApiKey);
            var model = _settings.Model ?? "gpt-3.5-turbo";
            _chatClient = _client.GetChatClient(model);
        }
    }

    public async Task<DomainChatResponse> SendMessageAsync(DomainChatRequest request, CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            return new DomainChatResponse { Error = "OpenAI client is not configured" };
        }

        try
        {
            var chatMessages = request.Messages.Select<DomainChatMessage, OpenAIChatMessage>(m => m.Role.ToLower() switch
            {
                "system" => OpenAIChatMessage.CreateSystemMessage(m.Content),
                "user" => OpenAIChatMessage.CreateUserMessage(m.Content),
                "assistant" => OpenAIChatMessage.CreateAssistantMessage(m.Content),
                _ => OpenAIChatMessage.CreateUserMessage(m.Content)
            }).ToList();

            var options = new ChatCompletionOptions
            {
                Temperature = (float)request.Temperature
            };
            
            if (request.MaxTokens.HasValue)
            {
                options.MaxOutputTokenCount = request.MaxTokens.Value;
            }

            var completion = await _chatClient.CompleteChatAsync(chatMessages, options, cancellationToken);
            
            return new DomainChatResponse
            {
                Content = completion.Value.Content[0].Text ?? string.Empty,
                Model = _settings?.Model,
                TokensUsed = completion.Value.Usage?.TotalTokenCount,
                IsComplete = true
            };
        }
        catch (Exception ex)
        {
            return new DomainChatResponse
            {
                Error = $"Error calling OpenAI: {ex.Message}",
                IsComplete = false
            };
        }
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(DomainChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            yield return "OpenAI client is not configured";
            yield break;
        }

        var chatMessages = request.Messages.Select<DomainChatMessage, OpenAIChatMessage>(m => m.Role.ToLower() switch
        {
            "system" => OpenAIChatMessage.CreateSystemMessage(m.Content),
            "user" => OpenAIChatMessage.CreateUserMessage(m.Content),
            "assistant" => OpenAIChatMessage.CreateAssistantMessage(m.Content),
            _ => OpenAIChatMessage.CreateUserMessage(m.Content)
        }).ToList();

        var options = new ChatCompletionOptions
        {
            Temperature = (float)request.Temperature
        };
        
        if (request.MaxTokens.HasValue)
        {
            options.MaxOutputTokenCount = request.MaxTokens.Value;
        }

        var streamingCompletion = _chatClient.CompleteChatStreamingAsync(chatMessages, options, cancellationToken);
        
        await foreach (var update in streamingCompletion)
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return contentPart.Text;
                }
            }
        }
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            return false;
        }

        try
        {
            var messages = new List<OpenAIChatMessage> { OpenAIChatMessage.CreateUserMessage("Hi") };
            var options = new ChatCompletionOptions { MaxOutputTokenCount = 1 };
            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            return response != null;
        }
        catch
        {
            return false;
        }
    }
}