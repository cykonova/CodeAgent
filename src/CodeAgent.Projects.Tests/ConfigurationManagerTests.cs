using CodeAgent.Projects.Models;
using CodeAgent.Projects.Services;
using CodeAgent.Projects.Templates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodeAgent.Projects.Tests;

public class ConfigurationManagerTests
{
    private readonly Mock<ILogger<ConfigurationManager>> _loggerMock;
    private readonly Mock<ITemplateProvider> _templateProviderMock;
    private readonly ConfigurationManager _sut;

    public ConfigurationManagerTests()
    {
        _loggerMock = new Mock<ILogger<ConfigurationManager>>();
        _templateProviderMock = new Mock<ITemplateProvider>();
        _sut = new ConfigurationManager(_loggerMock.Object, _templateProviderMock.Object);
    }

    [Fact]
    public void GetSystemDefaults_ShouldReturnDefaultConfiguration()
    {
        var result = _sut.GetSystemDefaults();

        result.Should().NotBeNull();
        result.Workflow.Should().NotBeNull();
        result.Workflow.Stages.Should().HaveCount(3);
        result.Agents.DefaultModel.Should().Be("gpt-4");
        result.CostLimits.EnableCostTracking.Should().BeTrue();
        result.Sandbox.SecurityLevel.Should().Be(SecurityLevel.Container);
    }

    [Fact]
    public void MergeConfigurations_ShouldMergeInOrder()
    {
        var config1 = new ProjectConfiguration 
        { 
            ProviderId = "provider1",
            Agents = new AgentConfiguration { DefaultModel = "model1" }
        };
        
        var config2 = new ProjectConfiguration 
        { 
            ProviderId = "provider2",
            CostLimits = new CostConfiguration { MaxCostPerRun = 10m }
        };
        
        var config3 = new ProjectConfiguration
        {
            Agents = new AgentConfiguration { DefaultModel = "model3" }
        };

        var result = _sut.MergeConfigurations(config1, config2, config3);

        result.ProviderId.Should().Be("provider2");
        result.Agents.DefaultModel.Should().Be("model3");
        result.CostLimits.MaxCostPerRun.Should().Be(10m);
    }

    [Fact]
    public void MergeConfigurations_WithNulls_ShouldHandleGracefully()
    {
        var config = new ProjectConfiguration { ProviderId = "test" };

        var result = _sut.MergeConfigurations(null, config, null);

        result.ProviderId.Should().Be("test");
    }

    [Fact]
    public void ApplyTemplate_ExistingTemplate_ShouldMergeWithBase()
    {
        var baseConfig = new ProjectConfiguration { ProviderId = "base" };
        var templateConfig = new ProjectConfiguration 
        { 
            Workflow = new WorkflowConfiguration { Name = "fast" }
        };
        
        _templateProviderMock
            .Setup(x => x.GetTemplate("fast"))
            .Returns(templateConfig);

        var result = _sut.ApplyTemplate(baseConfig, "fast");

        result.ProviderId.Should().Be("base");
        result.Workflow.Name.Should().Be("fast");
    }

    [Fact]
    public void ApplyTemplate_NonExistingTemplate_ShouldReturnBase()
    {
        var baseConfig = new ProjectConfiguration { ProviderId = "base" };
        
        _templateProviderMock
            .Setup(x => x.GetTemplate(It.IsAny<string>()))
            .Returns((ProjectConfiguration?)null);

        var result = _sut.ApplyTemplate(baseConfig, "nonexistent");

        result.Should().Be(baseConfig);
    }

    [Fact]
    public void ValidateConfiguration_ValidConfig_ShouldReturnTrue()
    {
        var config = new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration
            {
                Stages = new List<WorkflowStage>
                {
                    new() { Name = "Stage1", AgentType = "agent1" }
                }
            },
            Agents = new AgentConfiguration { MaxConcurrentAgents = 3 },
            CostLimits = new CostConfiguration { EnableCostTracking = false }
        };

        var result = _sut.ValidateConfiguration(config, out var errors);

        errors.Should().BeEmpty();
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfiguration_NoStages_ShouldReturnFalse()
    {
        var config = new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration { Stages = new List<WorkflowStage>() }
        };

        var result = _sut.ValidateConfiguration(config, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("Workflow must have at least one stage");
    }

    [Fact]
    public void ValidateConfiguration_StageWithoutName_ShouldReturnFalse()
    {
        var config = new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration
            {
                Stages = new List<WorkflowStage>
                {
                    new() { Name = "", AgentType = "agent1" }
                }
            }
        };

        var result = _sut.ValidateConfiguration(config, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("All workflow stages must have a name");
    }

    [Fact]
    public void ValidateConfiguration_CostTrackingWithoutLimits_ShouldReturnFalse()
    {
        var config = new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration
            {
                Stages = new List<WorkflowStage>
                {
                    new() { Name = "Stage1", AgentType = "agent1" }
                }
            },
            CostLimits = new CostConfiguration
            {
                EnableCostTracking = true,
                MaxCostPerRun = null,
                MaxCostPerDay = null,
                MaxCostPerMonth = null
            }
        };

        var result = _sut.ValidateConfiguration(config, out var errors);

        result.Should().BeFalse();
        errors.Should().Contain("At least one cost limit must be set when cost tracking is enabled");
    }

    [Fact]
    public async Task SaveAndLoadConfiguration_ShouldPersistAndRestore()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var config = new ProjectConfiguration
            {
                ProviderId = "test-provider",
                Workflow = new WorkflowConfiguration { Name = "test-workflow" }
            };

            await _sut.SaveConfigurationAsync(config, tempFile);
            var loaded = await _sut.LoadConfigurationAsync(tempFile);

            loaded.ProviderId.Should().Be(config.ProviderId);
            loaded.Workflow.Name.Should().Be(config.Workflow.Name);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}