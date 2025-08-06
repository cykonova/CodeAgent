using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests;

public class ProviderManagerTests
{
    private readonly ProviderManager _providerManager;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ProviderManager>> _loggerMock;

    public ProviderManagerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ProviderManager>>();
        
        _providerManager = new ProviderManager(
            _serviceProviderMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void RegisterProvider_WithValidProvider_AddsToCollection()
    {
        // Arrange
        var providerMock = new Mock<ILLMProvider>();
        providerMock.Setup(p => p.Name).Returns("TestProvider");

        // Act
        _providerManager.RegisterProvider("test", providerMock.Object);
        var providers = _providerManager.GetAvailableProviders();

        // Assert
        providers.Should().Contain("test");
        _providerManager.CurrentProviderName.Should().Be("test");
    }

    [Fact]
    public void RegisterProvider_WithNullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _providerManager.RegisterProvider("test", null!));
    }

    [Fact]
    public void RegisterProvider_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var providerMock = new Mock<ILLMProvider>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _providerManager.RegisterProvider("", providerMock.Object));
    }

    [Fact]
    public async Task SwitchProviderAsync_ToExistingProvider_ReturnsTrue()
    {
        // Arrange
        var providerMock = new Mock<ILLMProvider>();
        providerMock.Setup(p => p.SendMessageAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { Content = "OK" });
        
        _providerManager.RegisterProvider("test", providerMock.Object);

        // Act
        var result = await _providerManager.SwitchProviderAsync("test");

        // Assert
        result.Should().BeTrue();
        _providerManager.CurrentProviderName.Should().Be("test");
    }

    [Fact]
    public async Task SwitchProviderAsync_ToNonExistentProvider_ReturnsFalse()
    {
        // Act
        var result = await _providerManager.SwitchProviderAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestProviderAsync_WithWorkingProvider_ReturnsTrue()
    {
        // Arrange
        var providerMock = new Mock<ILLMProvider>();
        providerMock.Setup(p => p.SendMessageAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { Content = "OK" });
        
        _providerManager.RegisterProvider("test", providerMock.Object);

        // Act
        var result = await _providerManager.TestProviderAsync("test");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestProviderAsync_WithFailingProvider_ReturnsFalse()
    {
        // Arrange
        var providerMock = new Mock<ILLMProvider>();
        providerMock.Setup(p => p.SendMessageAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));
        
        _providerManager.RegisterProvider("test", providerMock.Object);

        // Act
        var result = await _providerManager.TestProviderAsync("test");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetProviderCapabilities_ReturnsCapabilities()
    {
        // Arrange
        var providerMock = new Mock<ILLMProvider>();
        _providerManager.RegisterProvider("test", providerMock.Object);

        // Act
        var capabilities = _providerManager.GetProviderCapabilities("test");

        // Assert
        capabilities.Should().NotBeNull();
        capabilities!.Name.Should().Be("test");
        capabilities.SupportsStreaming.Should().BeTrue();
    }

    [Fact]
    public void GetProviderCapabilities_ForNonExistentProvider_ReturnsNull()
    {
        // Act
        var capabilities = _providerManager.GetProviderCapabilities("nonexistent");

        // Assert
        capabilities.Should().BeNull();
    }

    [Fact]
    public async Task LoadProvidersAsync_LoadsEnabledProviders()
    {
        // Arrange
        var providersSection = new Mock<IConfigurationSection>();
        var providerConfig = new Mock<IConfigurationSection>();
        
        providerConfig.Setup(c => c.Key).Returns("TestProvider");
        providerConfig.Setup(c => c["Type"]).Returns("openai");
        providerConfig.Setup(c => c["Enabled"]).Returns("true");
        
        providersSection.Setup(s => s.GetChildren())
            .Returns(new[] { providerConfig.Object });
        
        _configurationMock.Setup(c => c.GetSection("Providers"))
            .Returns(providersSection.Object);
        
        _configurationMock.Setup(c => c["DefaultProvider"])
            .Returns((string?)null);

        var providerMock = new Mock<ILLMProvider>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILLMProvider)))
            .Returns(providerMock.Object);

        // Act
        await _providerManager.LoadProvidersAsync();

        // Assert
        var providers = _providerManager.GetAvailableProviders();
        providers.Should().Contain("TestProvider");
    }

    [Fact]
    public void CurrentProvider_ReturnsCurrentlyActiveProvider()
    {
        // Arrange
        var provider1 = new Mock<ILLMProvider>();
        var provider2 = new Mock<ILLMProvider>();
        
        _providerManager.RegisterProvider("provider1", provider1.Object);
        _providerManager.RegisterProvider("provider2", provider2.Object);

        // Act & Assert
        _providerManager.CurrentProvider.Should().Be(provider1.Object);
        _providerManager.CurrentProviderName.Should().Be("provider1");
    }
}