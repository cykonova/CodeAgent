using CodeAgent.Domain.Models;
using CodeAgent.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Infrastructure.Tests;

public class SessionServiceTests : IDisposable
{
    private readonly SessionService _sessionService;
    private readonly string _testSessionDir;
    private readonly Mock<ILogger<SessionService>> _loggerMock;

    public SessionServiceTests()
    {
        _testSessionDir = Path.Combine(Path.GetTempPath(), $"session-test-{Guid.NewGuid()}");
        _loggerMock = new Mock<ILogger<SessionService>>();
        _sessionService = new SessionService(_testSessionDir, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateSessionAsync_CreatesNewSession()
    {
        // Act
        var session = await _sessionService.CreateSessionAsync("Test Session");

        // Assert
        session.Should().NotBeNull();
        session.Id.Should().NotBeNullOrEmpty();
        session.Name.Should().Be("Test Session");
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        session.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSessionAsync_WithoutName_GeneratesDefaultName()
    {
        // Act
        var session = await _sessionService.CreateSessionAsync();

        // Assert
        session.Name.Should().StartWith("Session");
        session.Name.Should().Contain(DateTime.Now.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task SaveSessionAsync_AndLoadSessionAsync_PersistsSession()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Persist Test");
        session.Messages.Add(new SessionMessage
        {
            Role = "user",
            Content = "Test message"
        });

        // Act
        var saved = await _sessionService.SaveSessionAsync(session);
        var loaded = await _sessionService.LoadSessionAsync(session.Id);

        // Assert
        saved.Should().BeTrue();
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(session.Id);
        loaded.Name.Should().Be("Persist Test");
        loaded.Messages.Should().HaveCount(1);
        loaded.Messages[0].Content.Should().Be("Test message");
    }

    [Fact]
    public async Task LoadSessionAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var session = await _sessionService.LoadSessionAsync("invalid-id");

        // Assert
        session.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSessionAsync_RemovesSession()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Delete Test");
        await _sessionService.SaveSessionAsync(session);

        // Act
        var deleted = await _sessionService.DeleteSessionAsync(session.Id);
        var loaded = await _sessionService.LoadSessionAsync(session.Id);

        // Assert
        deleted.Should().BeTrue();
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSessionAsync_WithCurrentSession_ClearsCurrentSession()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Current Test");
        await _sessionService.SetCurrentSessionAsync(session);
        await _sessionService.SaveSessionAsync(session);

        // Act
        await _sessionService.DeleteSessionAsync(session.Id);
        var current = await _sessionService.GetCurrentSessionAsync();

        // Assert
        current.Should().BeNull();
    }

    [Fact]
    public async Task ListSessionsAsync_ReturnsAllSessions()
    {
        // Arrange
        var session1 = await _sessionService.CreateSessionAsync("Session 1");
        var session2 = await _sessionService.CreateSessionAsync("Session 2");
        await _sessionService.SaveSessionAsync(session1);
        await _sessionService.SaveSessionAsync(session2);

        // Act
        var sessions = await _sessionService.ListSessionsAsync();

        // Assert
        sessions.Should().HaveCount(2);
        sessions.Should().Contain(s => s.Name == "Session 1");
        sessions.Should().Contain(s => s.Name == "Session 2");
    }

    [Fact]
    public async Task ListSessionsAsync_OrdersByLastAccessedDate()
    {
        // Arrange
        var session1 = await _sessionService.CreateSessionAsync("Old Session");
        await _sessionService.SaveSessionAsync(session1);
        
        await Task.Delay(100);
        
        var session2 = await _sessionService.CreateSessionAsync("New Session");
        await _sessionService.SaveSessionAsync(session2);

        // Act
        var sessions = (await _sessionService.ListSessionsAsync()).ToList();

        // Assert
        sessions[0].Name.Should().Be("New Session");
        sessions[1].Name.Should().Be("Old Session");
    }

    [Fact]
    public async Task GetCurrentSessionAsync_AndSetCurrentSessionAsync_ManagesCurrentSession()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Current");

        // Act
        await _sessionService.SetCurrentSessionAsync(session);
        var current = await _sessionService.GetCurrentSessionAsync();

        // Assert
        current.Should().NotBeNull();
        current!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task AddMessageAsync_AddsMessageToSession()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Message Test");
        await _sessionService.SaveSessionAsync(session);
        var message = new SessionMessage
        {
            Role = "assistant",
            Content = "Response message"
        };

        // Act
        await _sessionService.AddMessageAsync(session.Id, message);
        var messages = await _sessionService.GetMessagesAsync(session.Id);

        // Assert
        messages.Should().HaveCount(1);
        messages.First().Content.Should().Be("Response message");
        messages.First().Role.Should().Be("assistant");
    }

    [Fact]
    public async Task AddMessageAsync_ToCurrentSession_AddsWithoutLoading()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Current Message Test");
        await _sessionService.SetCurrentSessionAsync(session);
        var message = new SessionMessage
        {
            Role = "user",
            Content = "User input"
        };

        // Act
        await _sessionService.AddMessageAsync(session.Id, message);
        var current = await _sessionService.GetCurrentSessionAsync();

        // Assert
        current!.Messages.Should().HaveCount(1);
        current.Messages[0].Content.Should().Be("User input");
    }

    [Fact]
    public async Task GetMessagesAsync_WithLimit_ReturnsLastNMessages()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Limit Test");
        for (int i = 1; i <= 5; i++)
        {
            session.Messages.Add(new SessionMessage
            {
                Role = "user",
                Content = $"Message {i}"
            });
        }
        await _sessionService.SaveSessionAsync(session);

        // Act
        var messages = await _sessionService.GetMessagesAsync(session.Id, limit: 2);

        // Assert
        messages.Should().HaveCount(2);
        messages.First().Content.Should().Be("Message 4");
        messages.Last().Content.Should().Be("Message 5");
    }

    [Fact]
    public async Task ClearSessionAsync_RemovesAllMessages()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Clear Test");
        session.Messages.Add(new SessionMessage { Role = "user", Content = "Message 1" });
        session.Messages.Add(new SessionMessage { Role = "assistant", Content = "Message 2" });
        session.Context["key"] = "value";
        await _sessionService.SaveSessionAsync(session);

        // Act
        await _sessionService.ClearSessionAsync(session.Id);
        var clearedSession = await _sessionService.LoadSessionAsync(session.Id);

        // Assert
        clearedSession!.Messages.Should().BeEmpty();
        clearedSession.Context.Should().BeEmpty();
    }

    [Fact]
    public async Task Session_WithComplexSettings_SerializesCorrectly()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Settings Test");
        session.Settings = new SessionSettings
        {
            Provider = "OpenAI",
            Model = "gpt-4",
            Temperature = 0.7,
            MaxTokens = 2000,
            CustomSettings = new Dictionary<string, object>
            {
                ["stream"] = true,
                ["top_p"] = 0.9
            }
        };

        // Act
        await _sessionService.SaveSessionAsync(session);
        var loaded = await _sessionService.LoadSessionAsync(session.Id);

        // Assert
        loaded!.Settings.Provider.Should().Be("OpenAI");
        loaded.Settings.Model.Should().Be("gpt-4");
        loaded.Settings.Temperature.Should().Be(0.7);
        loaded.Settings.MaxTokens.Should().Be(2000);
        loaded.Settings.CustomSettings.Should().ContainKey("stream");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testSessionDir))
        {
            Directory.Delete(_testSessionDir, true);
        }
    }
}