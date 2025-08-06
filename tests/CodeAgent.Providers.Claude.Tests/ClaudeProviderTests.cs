using System.Net;
using System.Text;
using System.Text.Json;
using CodeAgent.Domain.Models;
using CodeAgent.Providers.Claude;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace CodeAgent.Providers.Claude.Tests;

public class ClaudeProviderTests
{
    private readonly Mock<ILogger<ClaudeProvider>> _loggerMock;
    private readonly Mock<IOptions<ClaudeOptions>> _optionsMock;
    private readonly ClaudeOptions _options;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public ClaudeProviderTests()
    {
        _loggerMock = new Mock<ILogger<ClaudeProvider>>();
        _optionsMock = new Mock<IOptions<ClaudeOptions>>();
        _options = new ClaudeOptions
        {
            ApiKey = "test-api-key",
            DefaultModel = "claude-3-5-sonnet-20241022",
            BaseUrl = "https://api.anthropic.com/v1"
        };
        _optionsMock.Setup(x => x.Value).Returns(_options);

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);

        // Assert
        provider.Name.Should().Be("Claude");
        provider.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenApiKeyIsEmpty()
    {
        // Arrange
        _options.ApiKey = "";
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);

        // Act & Assert
        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnError_WhenNotConfigured()
    {
        // Arrange
        _options.ApiKey = "";
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);
        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>
            {
                new ChatMessage("user", "Hello")
            }
        };

        // Act
        var response = await provider.SendMessageAsync(request);

        // Assert
        response.IsComplete.Should().BeFalse();
        response.Error.Should().Be("Claude API key is not configured");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnSuccess_WhenApiCallSucceeds()
    {
        // Arrange
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);
        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>
            {
                new ChatMessage("user", "Hello")
            }
        };

        var apiResponse = new
        {
            content = new[]
            {
                new { text = "Hello! How can I help you?" }
            },
            model = "claude-3-5-sonnet-20241022",
            usage = new
            {
                input_tokens = 10,
                output_tokens = 20
            }
        };

        var responseContent = JsonSerializer.Serialize(apiResponse);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var response = await provider.SendMessageAsync(request);

        // Assert
        response.IsComplete.Should().BeTrue();
        response.Content.Should().Be("Hello! How can I help you?");
        response.Model.Should().Be("claude-3-5-sonnet-20241022");
        response.TokensUsed.Should().Be(30);
        response.Error.Should().BeNull();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldHandleApiError()
    {
        // Arrange
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);
        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>
            {
                new ChatMessage("user", "Hello")
            }
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized", Encoding.UTF8, "text/plain")
            });

        // Act
        var response = await provider.SendMessageAsync(request);

        // Assert
        response.IsComplete.Should().BeFalse();
        response.Error.Should().Contain("Claude API error");
    }

    [Fact]
    public async Task StreamMessageAsync_ShouldYieldError_WhenNotConfigured()
    {
        // Arrange
        _options.ApiKey = "";
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);
        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>
            {
                new ChatMessage("user", "Hello")
            },
            Stream = true
        };

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in provider.StreamMessageAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().ContainSingle();
        chunks[0].Should().Be("Error: Claude API key is not configured");
    }

    [Fact]
    public async Task ValidateConnectionAsync_ShouldReturnFalse_WhenNotConfigured()
    {
        // Arrange
        _options.ApiKey = "";
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);

        // Act
        var result = await provider.ValidateConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateConnectionAsync_ShouldReturnTrue_WhenConnectionSucceeds()
    {
        // Arrange
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);

        var apiResponse = new
        {
            content = new[]
            {
                new { text = "Hi" }
            },
            model = "claude-3-5-sonnet-20241022"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(apiResponse), Encoding.UTF8, "application/json")
            });

        // Act
        var result = await provider.ValidateConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SystemRole_ShouldBeConvertedToAssistant()
    {
        // Arrange
        var provider = new ClaudeProvider(_optionsMock.Object, _loggerMock.Object, _httpClient);
        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>
            {
                new ChatMessage("system", "You are a helpful assistant"),
                new ChatMessage("user", "Hello")
            }
        };

        var capturedRequest = string.Empty;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedRequest = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"content\":[{\"text\":\"Hi\"}],\"model\":\"claude\"}", Encoding.UTF8, "application/json")
            });

        // Act
        _ = await provider.SendMessageAsync(request);

        // Assert
        capturedRequest.Should().Contain("\"role\":\"assistant\"");
        capturedRequest.Should().NotContain("\"role\":\"system\"");
    }
}