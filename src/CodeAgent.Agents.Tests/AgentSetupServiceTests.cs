using CodeAgent.Agents;
using CodeAgent.Agents.Contracts;
using CodeAgent.Agents.Services;
using CodeAgent.Providers.Contracts;
using CodeAgent.Providers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Agents.Tests;

public class AgentSetupServiceTests
{
    private readonly Mock<IProviderRegistry> _providerRegistryMock;
    private readonly Mock<IAgentFactory> _agentFactoryMock;
    private readonly Mock<ILogger<AgentSetupService>> _loggerMock;
    private readonly AgentSetupService _setupService;

    public AgentSetupServiceTests()
    {
        _providerRegistryMock = new Mock<IProviderRegistry>();
        _agentFactoryMock = new Mock<IAgentFactory>();
        _loggerMock = new Mock<ILogger<AgentSetupService>>();
        _setupService = new AgentSetupService(
            _providerRegistryMock.Object,
            _agentFactoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SetupAgentsAsync_ShouldThrowException_WhenNoProvidersAvailable()
    {
        // Arrange
        _providerRegistryMock.Setup(x => x.GetAllProviders())
            .Returns(Enumerable.Empty<ILLMProvider>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _setupService.SetupAgentsAsync());
    }

    [Fact]
    public async Task SetupAgentsAsync_ShouldSetupAutomaticAgents_WhenNoCustomConfigurationProvided()
    {
        // Arrange
        var mockProvider = CreateMockProvider("provider1");
        _providerRegistryMock.Setup(x => x.GetAllProviders())
            .Returns(new[] { mockProvider });

        SetupAgentFactoryForAllTypes();

        // Act
        var agents = await _setupService.SetupAgentsAsync();

        // Assert
        agents.Should().NotBeNull();
        agents.Should().HaveCount(5); // Planning, Coding, Review, Testing, Documentation
        
        _agentFactoryMock.Verify(x => x.CreateAgentAsync(It.IsAny<AgentType>(), It.IsAny<CancellationToken>()), 
            Times.Exactly(5));
    }

    [Fact]
    public async Task SetupAgentsAsync_ShouldUseCorrectTemperatures_ForAutomaticSetup()
    {
        // Arrange
        var mockProvider = CreateMockProvider("provider1");
        _providerRegistryMock.Setup(x => x.GetAllProviders())
            .Returns(new[] { mockProvider });

        var capturedConfigurations = new List<AgentConfiguration>();
        
        SetupAgentFactoryWithCapture(capturedConfigurations);

        // Act
        await _setupService.SetupAgentsAsync();

        // Assert
        capturedConfigurations.Should().HaveCount(5);
        
        var planningConfig = capturedConfigurations.First(c => c.Type == AgentType.Planning);
        planningConfig.Temperature.Should().Be(0.7);
        
        var codingConfig = capturedConfigurations.First(c => c.Type == AgentType.Coding);
        codingConfig.Temperature.Should().Be(0.3);
        
        var reviewConfig = capturedConfigurations.First(c => c.Type == AgentType.Review);
        reviewConfig.Temperature.Should().Be(0.2);
        
        var testingConfig = capturedConfigurations.First(c => c.Type == AgentType.Testing);
        testingConfig.Temperature.Should().Be(0.1);
        
        var docConfig = capturedConfigurations.First(c => c.Type == AgentType.Documentation);
        docConfig.Temperature.Should().Be(0.5);
    }

    [Fact]
    public async Task SetupAgentsAsync_ShouldSetupCustomAgents_WhenCustomConfigurationProvided()
    {
        // Arrange
        var mockProvider1 = CreateMockProvider("provider1");
        var mockProvider2 = CreateMockProvider("provider2");
        
        _providerRegistryMock.Setup(x => x.GetAllProviders())
            .Returns(new[] { mockProvider1, mockProvider2 });
        
        _providerRegistryMock.Setup(x => x.GetProvider("provider1"))
            .Returns(mockProvider1);
        _providerRegistryMock.Setup(x => x.GetProvider("provider2"))
            .Returns(mockProvider2);

        var customConfig = new AgentSetupConfiguration
        {
            UseCustomAgentAssignment = true,
            AgentConfigurations = new List<AgentConfiguration>
            {
                new AgentConfiguration
                {
                    AgentId = "custom-planning",
                    Type = AgentType.Planning,
                    ProviderId = "provider1",
                    Temperature = 0.8
                },
                new AgentConfiguration
                {
                    AgentId = "custom-coding",
                    Type = AgentType.Coding,
                    ProviderId = "provider2",
                    Temperature = 0.4
                }
            }
        };

        var setupService = new AgentSetupService(
            _providerRegistryMock.Object,
            _agentFactoryMock.Object,
            _loggerMock.Object,
            customConfig);

        SetupAgentFactoryForAllTypes();

        // Act
        var agents = await setupService.SetupAgentsAsync();

        // Assert
        agents.Should().HaveCount(2);
        _agentFactoryMock.Verify(x => x.CreateAgentAsync(AgentType.Planning, It.IsAny<CancellationToken>()), Times.Once);
        _agentFactoryMock.Verify(x => x.CreateAgentAsync(AgentType.Coding, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetupAgentsAsync_ShouldSkipAgents_WhenProviderNotFound()
    {
        // Arrange
        var mockProvider = CreateMockProvider("provider1");
        
        _providerRegistryMock.Setup(x => x.GetAllProviders())
            .Returns(new[] { mockProvider });
        
        _providerRegistryMock.Setup(x => x.GetProvider("provider1"))
            .Returns(mockProvider);
        _providerRegistryMock.Setup(x => x.GetProvider("nonexistent"))
            .Returns((ILLMProvider?)null);

        var customConfig = new AgentSetupConfiguration
        {
            UseCustomAgentAssignment = true,
            AgentConfigurations = new List<AgentConfiguration>
            {
                new AgentConfiguration
                {
                    AgentId = "agent1",
                    Type = AgentType.Planning,
                    ProviderId = "provider1"
                },
                new AgentConfiguration
                {
                    AgentId = "agent2",
                    Type = AgentType.Coding,
                    ProviderId = "nonexistent"
                }
            }
        };

        var setupService = new AgentSetupService(
            _providerRegistryMock.Object,
            _agentFactoryMock.Object,
            _loggerMock.Object,
            customConfig);

        SetupAgentFactoryForAllTypes();

        // Act
        var agents = await setupService.SetupAgentsAsync();

        // Assert
        agents.Should().HaveCount(1);
        agents[0].Type.Should().Be(AgentType.Planning);
    }

    private ILLMProvider CreateMockProvider(string providerId)
    {
        var mockProvider = new Mock<ILLMProvider>();
        mockProvider.Setup(x => x.ProviderId).Returns(providerId);
        mockProvider.Setup(x => x.Name).Returns($"Provider {providerId}");
        return mockProvider.Object;
    }

    private void SetupAgentFactoryForAllTypes()
    {
        foreach (AgentType type in Enum.GetValues(typeof(AgentType)))
        {
            if (type != AgentType.Custom)
            {
                var mockAgent = new Mock<IAgent>();
                mockAgent.Setup(x => x.Type).Returns(type);
                mockAgent.Setup(x => x.InitializeAsync(It.IsAny<AgentConfiguration>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
                
                _agentFactoryMock.Setup(x => x.CreateAgentAsync(type, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockAgent.Object);
            }
        }
    }

    private void SetupAgentFactoryWithCapture(List<AgentConfiguration> capturedConfigurations)
    {
        foreach (AgentType type in Enum.GetValues(typeof(AgentType)))
        {
            if (type != AgentType.Custom)
            {
                var mockAgent = new Mock<IAgent>();
                mockAgent.Setup(x => x.Type).Returns(type);
                mockAgent.Setup(x => x.InitializeAsync(It.IsAny<AgentConfiguration>(), It.IsAny<CancellationToken>()))
                    .Callback<AgentConfiguration, CancellationToken>((config, ct) => capturedConfigurations.Add(config))
                    .ReturnsAsync(true);
                
                _agentFactoryMock.Setup(x => x.CreateAgentAsync(type, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockAgent.Object);
            }
        }
    }
}