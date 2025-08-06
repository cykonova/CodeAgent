using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using CodeAgent.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CodeAgent.Web.Tests;

public class ExportServiceTests
{
    private readonly ExportService _exportService;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger<ExportService>> _loggerMock;

    public ExportServiceTests()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _loggerMock = new Mock<ILogger<ExportService>>();
        _exportService = new ExportService(_sessionServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExportSessionAsync_Json_ExportsValidJson()
    {
        // Arrange
        var sessionId = "test-session";
        var session = CreateTestSession(sessionId);
        _sessionServiceMock.Setup(x => x.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _exportService.ExportSessionAsync(sessionId, "json");

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("json");
        result.ContentType.Should().Be("application/json");
        result.FileName.Should().Contain(sessionId).And.EndWith(".json");
        
        var json = System.Text.Encoding.UTF8.GetString(result.Data);
        var deserializedSession = JsonSerializer.Deserialize<Session>(json);
        deserializedSession.Should().NotBeNull();
        deserializedSession!.Id.Should().Be(sessionId);
    }

    [Fact]
    public async Task ExportSessionAsync_Markdown_ExportsValidMarkdown()
    {
        // Arrange
        var sessionId = "test-session";
        var session = CreateTestSession(sessionId);
        _sessionServiceMock.Setup(x => x.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _exportService.ExportSessionAsync(sessionId, "markdown");

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("markdown");
        result.ContentType.Should().Be("text/markdown");
        result.FileName.Should().Contain(sessionId).And.EndWith(".md");
        
        var markdown = System.Text.Encoding.UTF8.GetString(result.Data);
        markdown.Should().Contain("# Session:");
        markdown.Should().Contain("## Conversation");
        markdown.Should().Contain("User:");
        markdown.Should().Contain("Assistant:");
    }

    [Fact]
    public async Task ExportSessionAsync_Html_ExportsValidHtml()
    {
        // Arrange
        var sessionId = "test-session";
        var session = CreateTestSession(sessionId);
        _sessionServiceMock.Setup(x => x.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _exportService.ExportSessionAsync(sessionId, "html");

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("html");
        result.ContentType.Should().Be("text/html");
        result.FileName.Should().Contain(sessionId).And.EndWith(".html");
        
        var html = System.Text.Encoding.UTF8.GetString(result.Data);
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("<html");
        html.Should().Contain("</html>");
        html.Should().Contain("Session:");
    }

    [Fact]
    public async Task ExportSessionAsync_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var sessionId = "test-session";

        // Act & Assert
        var act = async () => await _exportService.ExportSessionAsync(sessionId, "invalid");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported export format*");
    }

    [Fact]
    public async Task ImportSessionAsync_Json_ImportsSuccessfully()
    {
        // Arrange
        var sessionId = "imported-session";
        var session = CreateTestSession(sessionId);
        var json = JsonSerializer.Serialize(session);
        var data = System.Text.Encoding.UTF8.GetBytes(json);

        _sessionServiceMock.Setup(x => x.CreateSessionAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session s, CancellationToken ct) => s);

        // Act
        var result = await _exportService.ImportSessionAsync(data, "json");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(sessionId);
        _sessionServiceMock.Verify(x => x.CreateSessionAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportSessionAsync_InvalidJson_ThrowsException()
    {
        // Arrange
        var data = System.Text.Encoding.UTF8.GetBytes("invalid json");

        // Act & Assert
        var act = async () => await _exportService.ImportSessionAsync(data, "json");
        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ExportAllSessionsAsync_ExportsMultipleSessions()
    {
        // Arrange
        var sessions = new List<Session>
        {
            CreateTestSession("session1"),
            CreateTestSession("session2"),
            CreateTestSession("session3")
        };
        _sessionServiceMock.Setup(x => x.GetAllSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _exportService.ExportAllSessionsAsync("json");

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("json");
        
        var json = System.Text.Encoding.UTF8.GetString(result.Data);
        var exportedData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        exportedData.Should().ContainKey("sessions");
        exportedData.Should().ContainKey("exportDate");
        exportedData.Should().ContainKey("version");
    }

    [Fact]
    public async Task ExportConfigurationAsync_ExportsSettings()
    {
        // Arrange
        var config = new Dictionary<string, object>
        {
            ["providers"] = new[] { "OpenAI", "Claude" },
            ["defaultModel"] = "gpt-4",
            ["maxTokens"] = 2000
        };

        // Act
        var result = await _exportService.ExportConfigurationAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("json");
        result.ContentType.Should().Be("application/json");
        result.FileName.Should().Contain("config").And.EndWith(".json");
        
        var json = System.Text.Encoding.UTF8.GetString(result.Data);
        json.Should().Contain("providers");
        json.Should().Contain("defaultModel");
    }

    [Fact]
    public async Task ImportConfigurationAsync_ImportsSettings()
    {
        // Arrange
        var config = new Dictionary<string, object>
        {
            ["providers"] = new[] { "OpenAI" },
            ["defaultModel"] = "gpt-4"
        };
        var json = JsonSerializer.Serialize(config);
        var data = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var result = await _exportService.ImportConfigurationAsync(data);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("providers");
        result.Should().ContainKey("defaultModel");
        result["defaultModel"].Should().Be("gpt-4");
    }

    private Session CreateTestSession(string sessionId)
    {
        return new Session
        {
            Id = sessionId,
            Title = $"Test Session {sessionId}",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello, how are you?" },
                new Message { Role = "assistant", Content = "I'm doing well, thank you!" },
                new Message { Role = "user", Content = "Can you help me with coding?" },
                new Message { Role = "assistant", Content = "Of course! What would you like help with?" }
            },
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "OpenAI",
                ["model"] = "gpt-4"
            },
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            LastModified = DateTime.UtcNow
        };
    }
}