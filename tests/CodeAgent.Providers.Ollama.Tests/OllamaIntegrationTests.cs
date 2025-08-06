using System.Text.Json;
using CodeAgent.Domain.Models;
using CodeAgent.Providers.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using System.Net;
using System.Text;

namespace CodeAgent.Providers.Ollama.Tests;

public class OllamaIntegrationTests
{
    [Fact]
    public async Task SendMessageAsync_WhenOllamaReturnsToolCallsInContent_ShouldParseToToolCalls()
    {
        // Arrange
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        
        var options = Options.Create(new OllamaOptions
        {
            BaseUrl = "http://localhost:11434",
            DefaultModel = "llama3.2"
        });
        var logger = new Mock<ILogger<OllamaProvider>>().Object;
        
        var provider = new OllamaProvider(options, logger, httpClient);
        
        // Mock Ollama response with tool calls in content (not in tool_calls property)
        var ollamaResponse = new
        {
            model = "llama3.2",
            message = new
            {
                role = "assistant",
                content = """{"name":"create_directory","parameters":{"path":"hotdog-stand"}}; {"name":"write_file","parameters":{"content":"Hello World","path":"index.html"}}"""
            },
            eval_count = 50
        };
        
        var responseJson = JsonSerializer.Serialize(ollamaResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };
        
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);
            
        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Create a hotdog stand app" }
            },
            Tools = new List<ToolDefinition>
            {
                new() { Name = "create_directory", Description = "Create directory" },
                new() { Name = "write_file", Description = "Write file" }
            }
        };

        // Act
        var result = await provider.SendMessageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.ToolCalls.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(2);
        
        var firstCall = result.ToolCalls![0];
        firstCall.Name.Should().Be("create_directory");
        firstCall.Arguments.Should().ContainKey("path");
        firstCall.Arguments["path"].Should().Be("hotdog-stand");
        
        var secondCall = result.ToolCalls![1];
        secondCall.Name.Should().Be("write_file");
        secondCall.Arguments.Should().ContainKey("content");
        secondCall.Arguments["content"].Should().Be("Hello World");
        secondCall.Arguments.Should().ContainKey("path");
        secondCall.Arguments["path"].Should().Be("index.html");
        
        // Should not return raw JSON as content
        result.Content.Should().BeNullOrEmpty();
    }
}