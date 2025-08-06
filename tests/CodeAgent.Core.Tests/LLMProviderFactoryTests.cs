using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeAgent.Core.Tests;

public class LLMProviderFactoryTests
{
    private readonly ServiceCollection _services;
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<ILLMProvider> _mockProvider1;
    private readonly Mock<ILLMProvider> _mockProvider2;

    public LLMProviderFactoryTests()
    {
        _services = new ServiceCollection();
        _mockProvider1 = new Mock<ILLMProvider>();
        _mockProvider1.Setup(x => x.Name).Returns("Provider1");
        
        _mockProvider2 = new Mock<ILLMProvider>();
        _mockProvider2.Setup(x => x.Name).Returns("Provider2");
        
        // Register the mock directly as ILLMProvider
        _services.AddSingleton<ILLMProvider>(_mockProvider1.Object);
        
        _serviceProvider = _services.BuildServiceProvider();
    }

    [Fact]
    public void RegisterProvider_ShouldAddProviderToRegistry()
    {
        // Arrange
        var factory = new LLMProviderFactory(_serviceProvider);
        
        // Act
        factory.RegisterProvider<ILLMProvider>("test-provider");
        
        // Assert
        factory.GetAvailableProviders().Should().Contain("test-provider");
    }

    [Fact]
    public void GetProvider_ShouldReturnRegisteredProvider()
    {
        // Arrange
        var factory = new LLMProviderFactory(_serviceProvider);
        factory.RegisterProvider<ILLMProvider>("provider1");
        
        // Act
        var provider = factory.GetProvider("provider1");
        
        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeAssignableTo<ILLMProvider>();
    }

    [Fact]
    public void GetProvider_ShouldThrowException_WhenProviderNotRegistered()
    {
        // Arrange
        var factory = new LLMProviderFactory(_serviceProvider);
        
        // Act
        var act = () => factory.GetProvider("unknown");
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Provider 'unknown' is not registered.");
    }

    [Fact]
    public void GetAvailableProviders_ShouldReturnAllRegisteredProviders()
    {
        // Arrange
        var factory = new LLMProviderFactory(_serviceProvider);
        factory.RegisterProvider<ILLMProvider>("provider1");
        factory.RegisterProvider<ILLMProvider>("provider2");
        
        // Act
        var providers = factory.GetAvailableProviders();
        
        // Assert
        providers.Should().HaveCount(2);
        providers.Should().Contain(new[] { "provider1", "provider2" });
    }

    [Fact]
    public void RegisterProvider_ShouldBeCaseInsensitive()
    {
        // Arrange
        var factory = new LLMProviderFactory(_serviceProvider);
        factory.RegisterProvider<ILLMProvider>("TestProvider");
        
        // Act
        var provider1 = factory.GetProvider("testprovider");
        var provider2 = factory.GetProvider("TESTPROVIDER");
        var provider3 = factory.GetProvider("TestProvider");
        
        // Assert
        provider1.Should().NotBeNull();
        provider2.Should().NotBeNull();
        provider3.Should().NotBeNull();
        provider1.Should().BeSameAs(provider2);
        provider2.Should().BeSameAs(provider3);
    }

    [Fact]
    public void RegisterProvider_ShouldOverwriteExistingProvider()
    {
        // Arrange
        var factory = new LLMProviderFactory(_serviceProvider);
        
        // Act
        factory.RegisterProvider<ILLMProvider>("test");
        factory.RegisterProvider<ILLMProvider>("test"); // Register again with same name
        
        // Assert
        factory.GetAvailableProviders().Count(p => p == "test").Should().Be(1);
    }
}