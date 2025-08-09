using CodeAgent.Agents.Base;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CodeAgent.Agents.Implementations;

public class CodingAgent : BaseAgent
{
    public CodingAgent(ILogger<CodingAgent> logger) : base(logger)
    {
    }

    protected override Task ConfigureCapabilitiesAsync(CancellationToken cancellationToken)
    {
        Capabilities = new AgentCapabilities
        {
            SupportsStreaming = true,
            SupportsParallelExecution = true,
            RequiresContext = true,
            MaxTokens = 8192,
            SupportedLanguages = new List<string> 
            { 
                "C#", "TypeScript", "JavaScript", "Python", "Java", 
                "Go", "Rust", "C++", "Swift", "Kotlin" 
            },
            SupportedFrameworks = new List<string> 
            { 
                ".NET", "Angular", "React", "Vue", "Node.js", 
                "Spring", "Django", "Flask", "Express" 
            }
        };
        
        return Task.CompletedTask;
    }

    protected override string GenerateSystemPrompt(AgentRequest request)
    {
        return @"You are a Coding Agent responsible for generating high-quality, production-ready code.

Your responsibilities include:
1. Writing clean, maintainable, and efficient code
2. Following best practices and design patterns
3. Implementing features according to specifications
4. Ensuring code is properly structured and documented
5. Handling edge cases and error scenarios

Guidelines:
- Write code that is self-documenting with clear variable names
- Include appropriate error handling
- Follow SOLID principles
- Implement unit testable code
- Use appropriate design patterns
- Add inline comments for complex logic
- Ensure thread safety where applicable
- Follow language-specific conventions and idioms

Output format:
- Provide complete, runnable code
- Include necessary imports/usings
- Add brief comments explaining key sections
- Specify the filename and path if applicable";
    }

    protected override string GenerateUserPrompt(AgentRequest request)
    {
        var prompt = $"Task: {request.Command}\n";
        
        if (!string.IsNullOrEmpty(request.Content))
        {
            prompt += $"Requirements:\n{request.Content}\n";
        }
        
        if (request.Context.Files.Any())
        {
            prompt += $"\nContext Files: {string.Join(", ", request.Context.Files)}\n";
        }
        
        if (request.Parameters.ContainsKey("language"))
        {
            prompt += $"Language: {request.Parameters["language"]}\n";
        }
        
        if (request.Parameters.ContainsKey("framework"))
        {
            prompt += $"Framework: {request.Parameters["framework"]}\n";
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
            
            var codeBlocks = ExtractCodeBlocks(content);
            
            foreach (var codeBlock in codeBlocks)
            {
                response.Artifacts.Add(new AgentArtifact
                {
                    Name = codeBlock.FileName ?? "code",
                    Type = ArtifactType.Code,
                    Content = codeBlock.Code,
                    Language = codeBlock.Language,
                    FilePath = codeBlock.FilePath ?? string.Empty
                });
            }
            
            response.Metadata["code_blocks_generated"] = codeBlocks.Count;
        }
        else
        {
            response.ErrorMessage = "Failed to generate code";
        }
        
        return response;
    }

    protected override double GetOptimalTemperature()
    {
        return _configuration?.Temperature ?? 0.3;
    }

    private List<CodeBlock> ExtractCodeBlocks(string content)
    {
        var codeBlocks = new List<CodeBlock>();
        var pattern = @"```(\w+)?\s*\n(.*?)\n```";
        var matches = Regex.Matches(content, pattern, RegexOptions.Singleline);
        
        foreach (Match match in matches)
        {
            var language = match.Groups[1].Value;
            var code = match.Groups[2].Value;
            
            var filePathPattern = @"//\s*(?:File|Filename|Path):\s*(.+)";
            var filePathMatch = Regex.Match(code, filePathPattern);
            
            string? filePath = null;
            string? fileName = null;
            
            if (filePathMatch.Success)
            {
                filePath = filePathMatch.Groups[1].Value.Trim();
                fileName = Path.GetFileName(filePath);
                code = Regex.Replace(code, filePathPattern, "").Trim();
            }
            
            codeBlocks.Add(new CodeBlock
            {
                Language = language,
                Code = code,
                FilePath = filePath,
                FileName = fileName
            });
        }
        
        return codeBlocks;
    }

    private class CodeBlock
    {
        public string Language { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
    }
}