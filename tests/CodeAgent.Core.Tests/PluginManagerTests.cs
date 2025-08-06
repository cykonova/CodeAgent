using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests;

public class PluginManagerTests : IDisposable
{
    private readonly PluginManager _pluginManager;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<PluginManager>> _loggerMock;
    private readonly string _testPluginDir;

    public PluginManagerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<PluginManager>>();
        _pluginManager = new PluginManager(_serviceProviderMock.Object, _loggerMock.Object);
        _testPluginDir = Path.Combine(Path.GetTempPath(), $"plugin-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPluginDir);
    }

    [Fact]
    public void RegisterPlugin_AddsPluginToCollection()
    {
        // Arrange
        var pluginMock = new Mock<IPlugin>();
        pluginMock.Setup(p => p.Id).Returns("test-plugin");
        pluginMock.Setup(p => p.Name).Returns("Test Plugin");
        pluginMock.Setup(p => p.Version).Returns("1.0.0");

        // Act
        _pluginManager.RegisterPlugin(pluginMock.Object);
        var plugins = _pluginManager.GetPlugins();

        // Assert
        plugins.Should().HaveCount(1);
        plugins.First().Id.Should().Be("test-plugin");
    }

    [Fact]
    public void RegisterPlugin_WithNullPlugin_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _pluginManager.RegisterPlugin(null!));
    }

    [Fact]
    public void RegisterPlugin_WithDuplicateId_DoesNotAddDuplicate()
    {
        // Arrange
        var plugin1 = new Mock<IPlugin>();
        plugin1.Setup(p => p.Id).Returns("test-plugin");
        
        var plugin2 = new Mock<IPlugin>();
        plugin2.Setup(p => p.Id).Returns("test-plugin");

        // Act
        _pluginManager.RegisterPlugin(plugin1.Object);
        _pluginManager.RegisterPlugin(plugin2.Object);
        var plugins = _pluginManager.GetPlugins();

        // Assert
        plugins.Should().HaveCount(1);
    }

    [Fact]
    public void GetPlugin_ReturnsCorrectPlugin()
    {
        // Arrange
        var pluginMock = new Mock<IPlugin>();
        pluginMock.Setup(p => p.Id).Returns("test-plugin");
        _pluginManager.RegisterPlugin(pluginMock.Object);

        // Act
        var plugin = _pluginManager.GetPlugin("test-plugin");

        // Assert
        plugin.Should().NotBeNull();
        plugin!.Id.Should().Be("test-plugin");
    }

    [Fact]
    public void GetPlugin_WithNonExistentId_ReturnsNull()
    {
        // Act
        var plugin = _pluginManager.GetPlugin("nonexistent");

        // Assert
        plugin.Should().BeNull();
    }

    [Fact]
    public async Task ExecutePluginAsync_WithValidPlugin_ReturnsResult()
    {
        // Arrange
        var pluginMock = new Mock<IPlugin>();
        pluginMock.Setup(p => p.Id).Returns("test-plugin");
        pluginMock.Setup(p => p.ExecuteAsync(It.IsAny<PluginContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PluginResult 
            { 
                Success = true, 
                Output = "Test output" 
            });
        
        _pluginManager.RegisterPlugin(pluginMock.Object);
        var context = new PluginContext();

        // Act
        var result = await _pluginManager.ExecutePluginAsync("test-plugin", context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Output.Should().Be("Test output");
    }

    [Fact]
    public async Task ExecutePluginAsync_WithNonExistentPlugin_ReturnsFailure()
    {
        // Arrange
        var context = new PluginContext();

        // Act
        var result = await _pluginManager.ExecutePluginAsync("nonexistent", context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Plugin not found");
    }

    [Fact]
    public async Task ExecutePluginAsync_WithThrowingPlugin_ReturnsFailure()
    {
        // Arrange
        var pluginMock = new Mock<IPlugin>();
        pluginMock.Setup(p => p.Id).Returns("test-plugin");
        pluginMock.Setup(p => p.ExecuteAsync(It.IsAny<PluginContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Plugin error"));
        
        _pluginManager.RegisterPlugin(pluginMock.Object);
        var context = new PluginContext();

        // Act
        var result = await _pluginManager.ExecutePluginAsync("test-plugin", context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Plugin error");
    }

    [Fact]
    public async Task UnloadPluginAsync_RemovesPlugin()
    {
        // Arrange
        var pluginMock = new Mock<IPlugin>();
        pluginMock.Setup(p => p.Id).Returns("test-plugin");
        pluginMock.Setup(p => p.ShutdownAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _pluginManager.RegisterPlugin(pluginMock.Object);

        // Act
        await _pluginManager.UnloadPluginAsync("test-plugin");
        var plugin = _pluginManager.GetPlugin("test-plugin");

        // Assert
        plugin.Should().BeNull();
        pluginMock.Verify(p => p.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnloadAllPluginsAsync_RemovesAllPlugins()
    {
        // Arrange
        var plugin1 = new Mock<IPlugin>();
        plugin1.Setup(p => p.Id).Returns("plugin1");
        plugin1.Setup(p => p.ShutdownAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var plugin2 = new Mock<IPlugin>();
        plugin2.Setup(p => p.Id).Returns("plugin2");
        plugin2.Setup(p => p.ShutdownAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _pluginManager.RegisterPlugin(plugin1.Object);
        _pluginManager.RegisterPlugin(plugin2.Object);

        // Act
        await _pluginManager.UnloadAllPluginsAsync();
        var plugins = _pluginManager.GetPlugins();

        // Assert
        plugins.Should().BeEmpty();
        plugin1.Verify(p => p.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
        plugin2.Verify(p => p.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadPluginsAsync_WithNonExistentDirectory_LogsWarning()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "nonexistent");

        // Act
        await _pluginManager.LoadPluginsAsync(nonExistentDir);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("does not exist")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPluginDir))
        {
            Directory.Delete(_testPluginDir, true);
        }
        _pluginManager?.Dispose();
    }
}