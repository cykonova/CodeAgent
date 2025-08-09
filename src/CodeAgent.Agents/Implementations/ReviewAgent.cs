using CodeAgent.Agents.Base;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Agents.Implementations;

public class ReviewAgent : BaseAgent
{
    public ReviewAgent(ILogger<ReviewAgent> logger) : base(logger)
    {
    }

    protected override Task ConfigureCapabilitiesAsync(CancellationToken cancellationToken)
    {
        Capabilities = new AgentCapabilities
        {
            SupportsStreaming = false,
            SupportsParallelExecution = true,
            RequiresContext = true,
            MaxTokens = 4096,
            SupportedLanguages = new List<string> { "all" },
            CustomCapabilities = new Dictionary<string, object>
            {
                ["security_review"] = true,
                ["performance_analysis"] = true,
                ["best_practices_check"] = true
            }
        };
        
        return Task.CompletedTask;
    }

    protected override string GenerateSystemPrompt(AgentRequest request)
    {
        return @"You are a Review Agent responsible for code quality analysis, security review, and best practices enforcement.

Your responsibilities include:
1. Code quality assessment
2. Security vulnerability detection
3. Performance analysis
4. Best practices verification
5. Architectural pattern validation

Review criteria:
- Code clarity and maintainability
- SOLID principles adherence
- Security vulnerabilities (OWASP Top 10)
- Performance bottlenecks
- Memory leaks and resource management
- Error handling completeness
- Test coverage assessment
- Documentation quality

Provide feedback in this structure:
1. Overall Assessment (score 1-10)
2. Strengths
3. Critical Issues (must fix)
4. Recommendations (should fix)
5. Minor Suggestions (nice to have)
6. Security Concerns (if any)
7. Performance Considerations

Be constructive and specific in your feedback.";
    }

    protected override string GenerateUserPrompt(AgentRequest request)
    {
        var prompt = $"Review Type: {request.Command}\n";
        
        if (!string.IsNullOrEmpty(request.Content))
        {
            prompt += $"Code/Content to Review:\n{request.Content}\n";
        }
        
        if (request.Parameters.ContainsKey("focus_areas"))
        {
            prompt += $"Focus Areas: {request.Parameters["focus_areas"]}\n";
        }
        
        if (request.Parameters.ContainsKey("severity_threshold"))
        {
            prompt += $"Severity Threshold: {request.Parameters["severity_threshold"]}\n";
        }
        
        return prompt;
    }

    protected override AgentResponse ProcessProviderResponse(ChatResponse providerResponse, AgentRequest request)
    {
        var content = providerResponse.Message?.Content ?? string.Empty;
        var response = new AgentResponse
        {
            Success = providerResponse.Message != null && !string.IsNullOrEmpty(content),
            Content = content,
            UpdatedContext = request.Context
        };
        
        if (response.Success)
        {
            response.UpdatedContext.TokensUsed += providerResponse.Usage?.TotalTokens ?? 0;
            
            var reviewResult = ParseReviewResult(content);
            
            response.Artifacts.Add(new AgentArtifact
            {
                Name = "review_report",
                Type = ArtifactType.Documentation,
                Content = content,
                Metadata = new Dictionary<string, object>
                {
                    ["score"] = reviewResult.Score,
                    ["critical_issues"] = reviewResult.CriticalIssues,
                    ["recommendations"] = reviewResult.Recommendations
                }
            });
            
            response.Metadata["review_score"] = reviewResult.Score;
            response.Metadata["has_critical_issues"] = reviewResult.CriticalIssues > 0;
        }
        else
        {
            response.ErrorMessage = "Failed to generate review";
        }
        
        return response;
    }

    protected override double GetOptimalTemperature()
    {
        return _configuration?.Temperature ?? 0.2;
    }

    private ReviewResult ParseReviewResult(string content)
    {
        var result = new ReviewResult();
        
        if (content.Contains("Overall Assessment"))
        {
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(
                content, @"Overall Assessment.*?(\d+)/10");
            if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var score))
            {
                result.Score = score;
            }
        }
        
        result.CriticalIssues = System.Text.RegularExpressions.Regex.Matches(
            content, @"Critical Issues.*?\n-").Count;
        
        result.Recommendations = System.Text.RegularExpressions.Regex.Matches(
            content, @"Recommendations.*?\n-").Count;
        
        return result;
    }

    private class ReviewResult
    {
        public int Score { get; set; } = 5;
        public int CriticalIssues { get; set; }
        public int Recommendations { get; set; }
    }
}