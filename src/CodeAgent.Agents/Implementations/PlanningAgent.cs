using CodeAgent.Agents.Base;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Agents.Implementations;

public class PlanningAgent : BaseAgent
{
    public PlanningAgent(ILogger<PlanningAgent> logger) : base(logger)
    {
    }

    protected override Task ConfigureCapabilitiesAsync(CancellationToken cancellationToken)
    {
        Capabilities = new AgentCapabilities
        {
            SupportsStreaming = true,
            SupportsParallelExecution = false,
            RequiresContext = true,
            MaxTokens = 8192,
            SupportedLanguages = new List<string> { "all" },
            SupportedFrameworks = new List<string> { "all" }
        };
        
        return Task.CompletedTask;
    }

    protected override string GenerateSystemPrompt(AgentRequest request)
    {
        return @"You are a Planning Agent responsible for analyzing requirements, designing architectures, and breaking down complex tasks into actionable steps.

Your responsibilities include:
1. Requirements analysis and clarification
2. System architecture design
3. Task decomposition and planning
4. Technical decision making
5. Risk assessment and mitigation strategies

When analyzing a request:
- Identify key requirements and constraints
- Propose appropriate architectural patterns
- Break down tasks into manageable components
- Consider scalability, maintainability, and performance
- Provide clear, actionable plans

Always structure your response with:
- Overview of the requirement
- Proposed approach
- Detailed task breakdown
- Success criteria
- Potential risks and mitigations";
    }

    protected override string GenerateUserPrompt(AgentRequest request)
    {
        var prompt = $"Command: {request.Command}\n";
        
        if (!string.IsNullOrEmpty(request.Content))
        {
            prompt += $"Details: {request.Content}\n";
        }
        
        if (request.Context.Files.Any())
        {
            prompt += $"Related Files: {string.Join(", ", request.Context.Files)}\n";
        }
        
        if (request.Parameters.Any())
        {
            prompt += "Parameters:\n";
            foreach (var param in request.Parameters)
            {
                prompt += $"  {param.Key}: {param.Value}\n";
            }
        }
        
        return prompt;
    }

    protected override AgentResponse ProcessProviderResponse(ChatResponse providerResponse, AgentRequest request)
    {
        var response = new AgentResponse
        {
            Success = providerResponse.Message != null && !string.IsNullOrEmpty(providerResponse.Message.Content),
            Content = providerResponse.Message?.Content ?? string.Empty,
            UpdatedContext = request.Context
        };
        
        if (response.Success)
        {
            response.UpdatedContext.TokensUsed += providerResponse.Usage?.TotalTokens ?? 0;
            
            response.Artifacts.Add(new AgentArtifact
            {
                Name = "plan",
                Type = ArtifactType.Documentation,
                Content = response.Content
            });
            
            response.Metadata["plan_generated"] = true;
        }
        else
        {
            response.ErrorMessage = "Failed to generate plan";
        }
        
        return response;
    }

    protected override double GetOptimalTemperature()
    {
        return _configuration?.Temperature ?? 0.7;
    }
}