using System.Reflection;
using CodeAgent.Providers.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CodeAgent.Providers.Ollama.Tests;

public class OllamaToolParsingTests
{
    [Fact]
    public void ParseToolCallsFromContent_ShouldParseMultipleToolCalls()
    {
        // Arrange
        var content = """{"name":"create_directory","parameters":{"path":"/src"}}; {"name":"write_file","parameters":{"content":"test","path":"index.html"}}""";
        
        var provider = CreateOllamaProvider();
        
        // Use reflection to access private method
        var method = typeof(OllamaProvider).GetMethod("ParseToolCallsFromContent", BindingFlags.NonPublic | BindingFlags.Static);
        
        // Act
        var result = (List<CodeAgent.Domain.Models.ToolCall>)method!.Invoke(null, new object[] { content })!;
        
        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("create_directory");
        result[0].Arguments.Should().ContainKey("path");
        result[0].Arguments["path"].Should().Be("/src");
        
        result[1].Name.Should().Be("write_file");
        result[1].Arguments.Should().ContainKey("content");
        result[1].Arguments["content"].Should().Be("test");
        result[1].Arguments.Should().ContainKey("path");
        result[1].Arguments["path"].Should().Be("index.html");
    }
    
    [Fact]
    public void ParseToolCallsFromContent_WithTrailingSemicolon_ShouldStillParse()
    {
        // Arrange
        var content = """{"name":"create_directory","parameters":{"path":"/src"}};""";
        
        var provider = CreateOllamaProvider();
        
        // Use reflection to access private method
        var method = typeof(OllamaProvider).GetMethod("ParseToolCallsFromContent", BindingFlags.NonPublic | BindingFlags.Static);
        
        // Act
        var result = (List<CodeAgent.Domain.Models.ToolCall>)method!.Invoke(null, new object[] { content })!;
        
        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("create_directory");
        result[0].Arguments["path"].Should().Be("/src");
    }
    
    private static OllamaProvider CreateOllamaProvider()
    {
        var options = Options.Create(new OllamaOptions
        {
            BaseUrl = "http://localhost:11434"
        });
        var logger = new Mock<ILogger<OllamaProvider>>().Object;
        var httpClient = new HttpClient();
        
        return new OllamaProvider(options, logger, httpClient);
    }
}