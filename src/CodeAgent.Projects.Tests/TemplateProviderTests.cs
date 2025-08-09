using CodeAgent.Projects.Models;
using CodeAgent.Projects.Templates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodeAgent.Projects.Tests;

public class TemplateProviderTests
{
    private readonly Mock<ILogger<TemplateProvider>> _loggerMock;
    private readonly TemplateProvider _sut;

    public TemplateProviderTests()
    {
        _loggerMock = new Mock<ILogger<TemplateProvider>>();
        _sut = new TemplateProvider(_loggerMock.Object);
    }

    [Fact]
    public void GetAvailableTemplates_ShouldReturnBuiltInTemplates()
    {
        var templates = _sut.GetAvailableTemplates();

        templates.Should().Contain(new[] { "standard", "fast", "quality", "budget" });
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("fast")]
    [InlineData("quality")]
    [InlineData("budget")]
    public void GetTemplate_BuiltInTemplate_ShouldReturnConfiguration(string templateName)
    {
        var template = _sut.GetTemplate(templateName);

        template.Should().NotBeNull();
        template!.Workflow.Should().NotBeNull();
        template.Workflow.Name.Should().Be(templateName);
    }

    [Fact]
    public void GetTemplate_StandardTemplate_ShouldHaveCorrectSettings()
    {
        var template = _sut.GetTemplate("standard");

        template.Should().NotBeNull();
        template!.Workflow.Stages.Should().HaveCount(3);
        template.Agents.DefaultModel.Should().Be("gpt-4");
        template.CostLimits.MaxCostPerRun.Should().Be(10m);
        template.Sandbox.SecurityLevel.Should().Be(SecurityLevel.Container);
    }

    [Fact]
    public void GetTemplate_FastTemplate_ShouldOptimizeForSpeed()
    {
        var template = _sut.GetTemplate("fast");

        template.Should().NotBeNull();
        template!.Workflow.Stages.Should().HaveCount(2);
        template.Workflow.AllowParallel.Should().BeTrue();
        template.Agents.DefaultModel.Should().Be("gpt-3.5-turbo");
        template.Agents.MaxConcurrentAgents.Should().Be(5);
        template.Sandbox.SecurityLevel.Should().Be(SecurityLevel.None);
    }

    [Fact]
    public void GetTemplate_QualityTemplate_ShouldHaveMoreStages()
    {
        var template = _sut.GetTemplate("quality");

        template.Should().NotBeNull();
        template!.Workflow.Stages.Should().HaveCount(6);
        template.Workflow.AllowParallel.Should().BeFalse();
        template.Agents.DefaultModel.Should().Be("gpt-4");
        template.CostLimits.MaxCostPerRun.Should().Be(25m);
        template.Sandbox.Resources.Memory.Should().Be("4G");
    }

    [Fact]
    public void GetTemplate_BudgetTemplate_ShouldMinimizeCosts()
    {
        var template = _sut.GetTemplate("budget");

        template.Should().NotBeNull();
        template!.Workflow.Stages.Should().HaveCount(2);
        template.Agents.DefaultModel.Should().Be("gpt-3.5-turbo");
        template.Agents.MaxConcurrentAgents.Should().Be(1);
        template.CostLimits.MaxCostPerRun.Should().Be(1m);
        template.CostLimits.MaxTokensPerRun.Should().Be(10000);
        template.Sandbox.Resources.Memory.Should().Be("512M");
    }

    [Fact]
    public void GetTemplate_CaseInsensitive_ShouldWork()
    {
        var template1 = _sut.GetTemplate("STANDARD");
        var template2 = _sut.GetTemplate("Standard");
        var template3 = _sut.GetTemplate("standard");

        template1.Should().NotBeNull();
        template2.Should().NotBeNull();
        template3.Should().NotBeNull();
        template1!.Workflow.Name.Should().Be(template3!.Workflow.Name);
    }

    [Fact]
    public void GetTemplate_NonExistent_ShouldReturnNull()
    {
        var template = _sut.GetTemplate("nonexistent");

        template.Should().BeNull();
    }

    [Fact]
    public void RegisterTemplate_ShouldAddNewTemplate()
    {
        var customConfig = new ProjectConfiguration
        {
            Workflow = new WorkflowConfiguration { Name = "custom" }
        };

        _sut.RegisterTemplate("custom", customConfig);
        var retrieved = _sut.GetTemplate("custom");

        retrieved.Should().Be(customConfig);
        _sut.GetAvailableTemplates().Should().Contain("custom");
    }

    [Fact]
    public void RegisterTemplate_DuplicateName_ShouldOverwrite()
    {
        var config1 = new ProjectConfiguration { ProviderId = "provider1" };
        var config2 = new ProjectConfiguration { ProviderId = "provider2" };

        _sut.RegisterTemplate("test", config1);
        _sut.RegisterTemplate("test", config2);

        var retrieved = _sut.GetTemplate("test");
        retrieved!.ProviderId.Should().Be("provider2");
    }

    [Fact]
    public void RemoveTemplate_ExistingTemplate_ShouldReturnTrue()
    {
        _sut.RegisterTemplate("temp", new ProjectConfiguration());

        var removed = _sut.RemoveTemplate("temp");

        removed.Should().BeTrue();
        _sut.GetTemplate("temp").Should().BeNull();
    }

    [Fact]
    public void RemoveTemplate_NonExistentTemplate_ShouldReturnFalse()
    {
        var removed = _sut.RemoveTemplate("nonexistent");

        removed.Should().BeFalse();
    }
}