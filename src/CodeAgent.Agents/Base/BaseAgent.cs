using CodeAgent.Agents.Contracts;
using CodeAgent.Providers.Contracts;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CodeAgent.Agents.Base;

public abstract class BaseAgent : IAgent
{
    protected ILLMProvider? _provider;
    protected AgentConfiguration? _configuration;
    protected readonly ILogger<BaseAgent> _logger;

    public string AgentId { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public AgentType Type { get; protected set; }
    public AgentCapabilities Capabilities { get; protected set; } = new();

    protected BaseAgent(ILogger<BaseAgent> logger)
    {
        _logger = logger;
    }

    public virtual async Task<bool> InitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _configuration = configuration;
        _provider = configuration.Provider;
        AgentId = configuration.AgentId;
        Name = configuration.Name;
        Type = configuration.Type;
        
        await ConfigureCapabilitiesAsync(cancellationToken);
        
        _logger.LogInformation("Agent {AgentId} ({Name}) initialized with type {Type}", AgentId, Name, Type);
        return true;
    }

    public virtual async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Agent {AgentId} executing request {RequestId}", AgentId, request.RequestId);
            
            var systemPrompt = GenerateSystemPrompt(request);
            var userPrompt = GenerateUserPrompt(request);
            
            var chatRequest = new ChatRequest
            {
                Model = _configuration?.Model ?? string.Empty,
                Temperature = GetOptimalTemperature(),
                MaxTokens = _configuration?.MaxTokens ?? 4096,
                SystemPrompt = systemPrompt,
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = userPrompt }
                }
            };
            
            AddContextMessages(chatRequest, request.Context);
            
            if (_provider == null)
            {
                throw new InvalidOperationException("Provider not initialized");
            }
            
            var chatResponse = await _provider.SendMessageAsync(chatRequest, cancellationToken);
            
            var response = ProcessProviderResponse(chatResponse, request);
            response.ExecutionTime = stopwatch.Elapsed;
            response.RequestId = request.RequestId;
            response.AgentId = AgentId;
            
            _logger.LogInformation("Agent {AgentId} completed request {RequestId} in {ElapsedMs}ms", 
                AgentId, request.RequestId, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} failed to execute request {RequestId}", AgentId, request.RequestId);
            
            return new AgentResponse
            {
                RequestId = request.RequestId,
                AgentId = AgentId,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    public virtual Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Agent {AgentId} shutting down", AgentId);
        return Task.CompletedTask;
    }

    protected abstract Task ConfigureCapabilitiesAsync(CancellationToken cancellationToken);
    protected abstract string GenerateSystemPrompt(AgentRequest request);
    protected abstract string GenerateUserPrompt(AgentRequest request);
    protected abstract AgentResponse ProcessProviderResponse(ChatResponse providerResponse, AgentRequest request);
    protected abstract double GetOptimalTemperature();

    protected virtual void AddContextMessages(ChatRequest chatRequest, AgentContext context)
    {
        foreach (var message in context.History.Take(10))
        {
            chatRequest.Messages.Insert(0, new ChatMessage
            {
                Role = message.Role,
                Content = message.Content
            });
        }
    }
}