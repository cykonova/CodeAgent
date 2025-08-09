using CodeAgent.Agents.Base;
using CodeAgent.Providers.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Agents.Implementations;

public class TestingAgent : BaseAgent
{
    public TestingAgent(ILogger<TestingAgent> logger) : base(logger)
    {
    }

    protected override Task ConfigureCapabilitiesAsync(CancellationToken cancellationToken)
    {
        Capabilities = new AgentCapabilities
        {
            SupportsStreaming = true,
            SupportsParallelExecution = true,
            RequiresContext = true,
            MaxTokens = 6144,
            SupportedLanguages = new List<string> 
            { 
                "C#", "TypeScript", "JavaScript", "Python", "Java", "Go" 
            },
            SupportedFrameworks = new List<string> 
            { 
                "xUnit", "NUnit", "MSTest", "Jest", "Mocha", "Jasmine", 
                "PyTest", "JUnit", "GoTest" 
            },
            CustomCapabilities = new Dictionary<string, object>
            {
                ["unit_tests"] = true,
                ["integration_tests"] = true,
                ["e2e_tests"] = true,
                ["performance_tests"] = true
            }
        };
        
        return Task.CompletedTask;
    }

    protected override string GenerateSystemPrompt(AgentRequest request)
    {
        return @"You are a Testing Agent responsible for generating comprehensive test suites and ensuring code quality through testing.

Your responsibilities include:
1. Unit test generation
2. Integration test design
3. Test coverage analysis
4. Edge case identification
5. Test data generation
6. Performance test scenarios

Testing principles:
- Follow AAA pattern (Arrange, Act, Assert)
- Test one thing per test
- Use descriptive test names
- Include positive and negative test cases
- Test edge cases and boundary conditions
- Mock external dependencies appropriately
- Ensure tests are deterministic
- Generate meaningful test data

Test structure:
- Setup/Teardown methods when needed
- Clear test organization
- Appropriate use of test fixtures
- Parameterized tests where applicable
- Performance benchmarks when relevant

Output complete, runnable test code with all necessary imports and setup.";
    }

    protected override string GenerateUserPrompt(AgentRequest request)
    {
        var prompt = $"Testing Task: {request.Command}\n";
        
        if (!string.IsNullOrEmpty(request.Content))
        {
            prompt += $"Code to Test:\n{request.Content}\n";
        }
        
        if (request.Parameters.ContainsKey("test_type"))
        {
            prompt += $"Test Type: {request.Parameters["test_type"]}\n";
        }
        
        if (request.Parameters.ContainsKey("framework"))
        {
            prompt += $"Testing Framework: {request.Parameters["framework"]}\n";
        }
        
        if (request.Parameters.ContainsKey("coverage_target"))
        {
            prompt += $"Coverage Target: {request.Parameters["coverage_target"]}%\n";
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
            
            var testCode = ExtractTestCode(content);
            var testMetrics = AnalyzeTestMetrics(testCode);
            
            response.Artifacts.Add(new AgentArtifact
            {
                Name = "test_suite",
                Type = ArtifactType.Test,
                Content = testCode,
                Metadata = new Dictionary<string, object>
                {
                    ["test_count"] = testMetrics.TestCount,
                    ["assertion_count"] = testMetrics.AssertionCount,
                    ["has_setup"] = testMetrics.HasSetup,
                    ["has_teardown"] = testMetrics.HasTeardown
                }
            });
            
            response.Metadata["tests_generated"] = testMetrics.TestCount;
            response.Metadata["test_framework"] = testMetrics.Framework;
        }
        else
        {
            response.ErrorMessage = "Failed to generate tests";
        }
        
        return response;
    }

    protected override double GetOptimalTemperature()
    {
        return _configuration?.Temperature ?? 0.1;
    }

    private string ExtractTestCode(string content)
    {
        var codeBlockPattern = @"```(?:\w+)?\s*\n(.*?)\n```";
        var match = System.Text.RegularExpressions.Regex.Match(
            content, codeBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        return match.Success ? match.Groups[1].Value : content;
    }

    private TestMetrics AnalyzeTestMetrics(string testCode)
    {
        var metrics = new TestMetrics();
        
        var testPatterns = new[]
        {
            @"\[Test\]|\[Fact\]|\[TestMethod\]|@Test|def test_|it\(|test\(",
            @"\.toBe|\.toEqual|Assert\.|assert|expect\(|should\."
        };
        
        metrics.TestCount = System.Text.RegularExpressions.Regex.Matches(
            testCode, testPatterns[0]).Count;
        
        metrics.AssertionCount = System.Text.RegularExpressions.Regex.Matches(
            testCode, testPatterns[1]).Count;
        
        metrics.HasSetup = testCode.Contains("Setup") || testCode.Contains("BeforeEach") || 
                          testCode.Contains("setUp") || testCode.Contains("@Before");
        
        metrics.HasTeardown = testCode.Contains("Teardown") || testCode.Contains("AfterEach") || 
                             testCode.Contains("tearDown") || testCode.Contains("@After");
        
        if (testCode.Contains("[Test]") || testCode.Contains("[Fact]"))
            metrics.Framework = "xUnit/NUnit";
        else if (testCode.Contains("@Test"))
            metrics.Framework = "JUnit";
        else if (testCode.Contains("def test_"))
            metrics.Framework = "PyTest";
        else if (testCode.Contains("describe(") || testCode.Contains("it("))
            metrics.Framework = "Jest/Mocha";
        
        return metrics;
    }

    private class TestMetrics
    {
        public int TestCount { get; set; }
        public int AssertionCount { get; set; }
        public bool HasSetup { get; set; }
        public bool HasTeardown { get; set; }
        public string Framework { get; set; } = "Unknown";
    }
}