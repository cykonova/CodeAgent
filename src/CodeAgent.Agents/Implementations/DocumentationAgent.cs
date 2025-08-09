using CodeAgent.Agents.Base;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Agents.Implementations;

public class DocumentationAgent : BaseAgent
{
    public DocumentationAgent(ILogger<DocumentationAgent> logger) : base(logger)
    {
    }

    protected override Task ConfigureCapabilitiesAsync(CancellationToken cancellationToken)
    {
        Capabilities = new AgentCapabilities
        {
            SupportsStreaming = true,
            SupportsParallelExecution = true,
            RequiresContext = false,
            MaxTokens = 4096,
            SupportedLanguages = new List<string> { "all" },
            CustomCapabilities = new Dictionary<string, object>
            {
                ["api_documentation"] = true,
                ["user_guides"] = true,
                ["code_comments"] = true,
                ["readme_generation"] = true,
                ["changelog_generation"] = true
            }
        };
        
        return Task.CompletedTask;
    }

    protected override string GenerateSystemPrompt(AgentRequest request)
    {
        return @"You are a Documentation Agent responsible for creating clear, comprehensive, and user-friendly documentation.

Your responsibilities include:
1. API documentation generation
2. User guide creation
3. Code documentation and comments
4. README file generation
5. Architecture documentation
6. Changelog maintenance

Documentation principles:
- Write for your audience (developers, users, or both)
- Use clear, concise language
- Include practical examples
- Structure content logically
- Provide complete information
- Use consistent formatting
- Include diagrams where helpful

Documentation formats:
- Markdown for general documentation
- XML comments for code documentation
- JSDoc/TSDoc for JavaScript/TypeScript
- Docstrings for Python
- Javadoc for Java
- XML documentation comments for C#

Always include:
- Purpose and overview
- Prerequisites/requirements
- Installation/setup instructions
- Usage examples
- API reference (if applicable)
- Troubleshooting section
- Contributing guidelines (for open source)";
    }

    protected override string GenerateUserPrompt(AgentRequest request)
    {
        var prompt = $"Documentation Task: {request.Command}\n";
        
        if (!string.IsNullOrEmpty(request.Content))
        {
            prompt += $"Content to Document:\n{request.Content}\n";
        }
        
        if (request.Parameters.ContainsKey("doc_type"))
        {
            prompt += $"Documentation Type: {request.Parameters["doc_type"]}\n";
        }
        
        if (request.Parameters.ContainsKey("target_audience"))
        {
            prompt += $"Target Audience: {request.Parameters["target_audience"]}\n";
        }
        
        if (request.Parameters.ContainsKey("format"))
        {
            prompt += $"Format: {request.Parameters["format"]}\n";
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
            
            var docType = DetermineDocumentationType(request, content);
            var docMetrics = AnalyzeDocumentation(content);
            
            response.Artifacts.Add(new AgentArtifact
            {
                Name = GetDocumentName(docType),
                Type = ArtifactType.Documentation,
                Content = content,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = docType,
                    ["word_count"] = docMetrics.WordCount,
                    ["sections"] = docMetrics.SectionCount,
                    ["has_examples"] = docMetrics.HasExamples,
                    ["has_toc"] = docMetrics.HasTableOfContents
                }
            });
            
            response.Metadata["documentation_type"] = docType;
            response.Metadata["completeness_score"] = docMetrics.CompletenessScore;
        }
        else
        {
            response.ErrorMessage = "Failed to generate documentation";
        }
        
        return response;
    }

    protected override double GetOptimalTemperature()
    {
        return _configuration?.Temperature ?? 0.5;
    }

    private string DetermineDocumentationType(AgentRequest request, string content)
    {
        if (request.Parameters.ContainsKey("doc_type"))
        {
            return request.Parameters["doc_type"].ToString() ?? "general";
        }
        
        if (content.Contains("## API Reference") || content.Contains("### Endpoints"))
            return "api";
        if (content.Contains("## Installation") || content.Contains("## Getting Started"))
            return "readme";
        if (content.Contains("## User Guide") || content.Contains("## How to"))
            return "user_guide";
        if (content.Contains("## Architecture") || content.Contains("## System Design"))
            return "architecture";
        
        return "general";
    }

    private string GetDocumentName(string docType)
    {
        return docType switch
        {
            "api" => "api_documentation",
            "readme" => "README",
            "user_guide" => "user_guide",
            "architecture" => "architecture_doc",
            "changelog" => "CHANGELOG",
            _ => "documentation"
        };
    }

    private DocumentationMetrics AnalyzeDocumentation(string content)
    {
        var metrics = new DocumentationMetrics();
        
        metrics.WordCount = content.Split(new[] { ' ', '\n', '\r', '\t' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;
        
        metrics.SectionCount = System.Text.RegularExpressions.Regex.Matches(
            content, @"^#{1,3}\s+.+$", System.Text.RegularExpressions.RegexOptions.Multiline).Count;
        
        metrics.HasExamples = content.Contains("```") || content.Contains("Example:") || 
                              content.Contains("example:");
        
        metrics.HasTableOfContents = content.Contains("## Table of Contents") || 
                                     content.Contains("## Contents") ||
                                     content.Contains("- [");
        
        var score = 50;
        if (metrics.SectionCount > 3) score += 20;
        if (metrics.HasExamples) score += 20;
        if (metrics.HasTableOfContents) score += 10;
        
        metrics.CompletenessScore = Math.Min(100, score);
        
        return metrics;
    }

    private class DocumentationMetrics
    {
        public int WordCount { get; set; }
        public int SectionCount { get; set; }
        public bool HasExamples { get; set; }
        public bool HasTableOfContents { get; set; }
        public int CompletenessScore { get; set; }
    }
}