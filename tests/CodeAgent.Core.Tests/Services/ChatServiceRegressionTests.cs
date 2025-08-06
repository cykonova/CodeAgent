using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests.Services;

public class ChatServiceRegressionTests
{
    private readonly Mock<ILogger<ChatService>> _loggerMock;
    private readonly Mock<ILLMProvider> _llmProviderMock;
    private readonly Mock<IInternalToolService> _toolServiceMock;
    private readonly Mock<IConfigurationService> _configServiceMock;
    private readonly ChatService _sut;

    public ChatServiceRegressionTests()
    {
        _loggerMock = new Mock<ILogger<ChatService>>();
        _llmProviderMock = new Mock<ILLMProvider>();
        _toolServiceMock = new Mock<IInternalToolService>();
        _configServiceMock = new Mock<IConfigurationService>();
        
        // Setup tools mock to be available during construction
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "write_file",
                Description = "Write content to a file",
                Parameters = new Dictionary<string, ParameterDefinition>()
            }
        };
        _toolServiceMock.Setup(x => x.GetAvailableTools()).Returns(tools);
        
        _sut = new ChatService(_llmProviderMock.Object, _toolServiceMock.Object, _configServiceMock.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenToolCallsPresent_ShouldExecuteToolsAndReturnSummary()
    {
        // Arrange
        var userMessage = "Create a test file";
        
        // Mock tool definitions
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "write_file",
                Description = "Write content to a file",
                Parameters = new Dictionary<string, ParameterDefinition>
                {
                    ["path"] = new() { Type = "string", Required = true },
                    ["content"] = new() { Type = "string", Required = true }
                }
            }
        };
        _toolServiceMock.Setup(x => x.GetAvailableTools()).Returns(tools);
        
        // Mock initial LLM response with tool calls
        var toolCalls = new List<ToolCall>
        {
            new()
            {
                Id = "call_1",
                Name = "write_file",
                Arguments = new Dictionary<string, object>
                {
                    ["path"] = "test.txt",
                    ["content"] = "Hello World"
                }
            }
        };
        
        var initialResponse = new ChatResponse
        {
            ToolCalls = toolCalls,
            IsComplete = true
        };
        
        // Mock follow-up LLM response with summary
        var summaryResponse = new ChatResponse
        {
            Content = "Created test.txt with 'Hello World' content. The file has been successfully written to your current directory.",
            IsComplete = true
        };
        
        // Setup LLM provider to return tool calls first, then summary
        _llmProviderMock.SetupSequence(x => x.SendMessageAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialResponse)
            .ReturnsAsync(summaryResponse);
        
        // Mock tool execution
        var toolResult = new ToolResult
        {
            ToolCallId = "call_1",
            Success = true,
            Content = "Successfully wrote 11 characters to test.txt"
        };
        _toolServiceMock.Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolResult);

        // Act
        var result = await _sut.ProcessMessageAsync(userMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.Content.Should().NotBeNull();
        result.Content.Should().Contain("Created test.txt");
        result.Content.Should().NotContain("write_file"); // Should not contain raw tool names
        result.Content.Should().NotContain("{\"name\":"); // Should not contain raw JSON
        
        // Verify tool was executed
        _toolServiceMock.Verify(x => x.ExecuteToolAsync(
            It.Is<ToolCall>(tc => tc.Name == "write_file" && tc.Arguments["path"].ToString() == "test.txt"), 
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify LLM was called twice (initial + follow-up)
        _llmProviderMock.Verify(x => x.SendMessageAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenOllamaReturnsRawJson_ShouldNotDisplayRawJson()
    {
        // Arrange - This test specifically captures the Ollama regression
        var userMessage = "lets build a hotdog stand pos web app";
        
        var tools = new List<ToolDefinition>
        {
            new() { Name = "create_directory", Description = "Create directory" },
            new() { Name = "write_file", Description = "Write file" }
        };
        _toolServiceMock.Setup(x => x.GetAvailableTools()).Returns(tools);
        
        // Mock Ollama returning raw JSON in content instead of proper tool calls
        var badResponse = new ChatResponse
        {
            Content = """{"name":"create_directory","parameters":{"path":"/src"}}; {"name":"write_file","parameters":{"content":"test","path":"index.html"}}""",
            IsComplete = true,
            ToolCalls = null // No proper tool calls, just raw JSON in content
        };
        
        _llmProviderMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(badResponse);

        // Act
        var result = await _sut.ProcessMessageAsync(userMessage);

        // Assert - Should not return raw JSON to user
        result.Content.Should().NotContain("""{"name":""");
        result.Content.Should().NotContain("parameters");
        
        // The response should either:
        // 1. Parse and execute the tools (preferred), OR
        // 2. Ask for clarification, OR  
        // 3. Provide a helpful message
        // But it should NEVER show raw JSON to the user
        result.Content.Should().NotBeNullOrEmpty();
    }
}