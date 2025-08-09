using CodeAgent.Projects.Interfaces;
using CodeAgent.Projects.Models;
using CodeAgent.Projects.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodeAgent.Projects.Tests;

public class ProjectServiceTests
{
    private readonly Mock<ILogger<ProjectService>> _loggerMock;
    private readonly Mock<IConfigurationManager> _configManagerMock;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _loggerMock = new Mock<ILogger<ProjectService>>();
        _configManagerMock = new Mock<IConfigurationManager>();
        _configManagerMock.Setup(x => x.GetDefaultConfiguration()).Returns(new ProjectConfiguration());
        _sut = new ProjectService(_loggerMock.Object, _configManagerMock.Object);
    }

    [Fact]
    public async Task CreateProjectAsync_ShouldCreateNewProject()
    {
        var name = "TestProject";
        var config = new ProjectConfiguration { ProviderId = "test-provider" };

        var result = await _sut.CreateProjectAsync(name, config);

        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Configuration.Should().Be(config);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateProjectAsync_WithoutConfig_ShouldUseDefaults()
    {
        var defaultConfig = new ProjectConfiguration { ProviderId = "default" };
        _configManagerMock.Setup(x => x.GetDefaultConfiguration()).Returns(defaultConfig);

        var result = await _sut.CreateProjectAsync("TestProject");

        result.Configuration.Should().Be(defaultConfig);
        _configManagerMock.Verify(x => x.GetDefaultConfiguration(), Times.Once);
    }

    [Fact]
    public async Task GetProjectAsync_ExistingProject_ShouldReturnProject()
    {
        var project = await _sut.CreateProjectAsync("TestProject");

        var result = await _sut.GetProjectAsync(project.Id);

        result.Should().NotBeNull();
        result.Should().Be(project);
    }

    [Fact]
    public async Task GetProjectAsync_NonExistingProject_ShouldReturnNull()
    {
        var result = await _sut.GetProjectAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProjectAsync_ShouldUpdateConfiguration()
    {
        var project = await _sut.CreateProjectAsync("TestProject");
        var newConfig = new ProjectConfiguration { ProviderId = "updated-provider" };

        var result = await _sut.UpdateProjectAsync(project.Id, newConfig);

        result.Configuration.Should().Be(newConfig);
        result.UpdatedAt.Should().BeAfter(result.CreatedAt);
    }

    [Fact]
    public async Task UpdateProjectAsync_NonExistingProject_ShouldThrow()
    {
        var act = () => _sut.UpdateProjectAsync(Guid.NewGuid(), new ProjectConfiguration());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteProjectAsync_ExistingProject_ShouldReturnTrue()
    {
        var project = await _sut.CreateProjectAsync("TestProject");

        var result = await _sut.DeleteProjectAsync(project.Id);

        result.Should().BeTrue();
        var deletedProject = await _sut.GetProjectAsync(project.Id);
        deletedProject.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProjectAsync_NonExistingProject_ShouldReturnFalse()
    {
        var result = await _sut.DeleteProjectAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CloneProjectAsync_ShouldCreateCopyWithNewId()
    {
        var sourceProject = await _sut.CreateProjectAsync("Original", 
            new ProjectConfiguration { ProviderId = "test" });
        sourceProject.Description = "Test description";
        sourceProject.Metadata["key"] = "value";

        var cloned = await _sut.CloneProjectAsync(sourceProject.Id, "Clone");

        cloned.Should().NotBeNull();
        cloned.Id.Should().NotBe(sourceProject.Id);
        cloned.Name.Should().Be("Clone");
        cloned.Description.Should().Be(sourceProject.Description);
        cloned.Configuration.ProviderId.Should().Be(sourceProject.Configuration.ProviderId);
        cloned.Metadata.Should().ContainKey("key");
    }

    [Fact]
    public async Task ApplyTemplateAsync_ShouldUpdateProjectWithTemplate()
    {
        var project = await _sut.CreateProjectAsync("TestProject");
        var templateConfig = new ProjectConfiguration 
        { 
            ProviderId = "template-provider",
            Workflow = new WorkflowConfiguration { Name = "fast" }
        };
        
        _configManagerMock
            .Setup(x => x.ApplyTemplate(It.IsAny<ProjectConfiguration>(), "fast"))
            .Returns(templateConfig);

        var result = await _sut.ApplyTemplateAsync(project.Id, "fast");

        result.TemplateName.Should().Be("fast");
        result.Type.Should().Be(ProjectType.Fast);
        result.Configuration.Should().Be(templateConfig);
    }

    [Fact]
    public async Task GetProjectStateAsync_NewProject_ShouldReturnInitialState()
    {
        var project = await _sut.CreateProjectAsync("TestProject");

        var state = await _sut.GetProjectStateAsync(project.Id);

        state.Should().NotBeNull();
        state.Status.Should().Be(ProjectStatus.Idle);
        state.RunHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateProjectStateAsync_ShouldUpdateState()
    {
        var project = await _sut.CreateProjectAsync("TestProject");
        var newState = new ProjectState
        {
            Status = ProjectStatus.Running,
            CurrentStage = "Implementation"
        };

        await _sut.UpdateProjectStateAsync(project.Id, newState);
        var retrievedState = await _sut.GetProjectStateAsync(project.Id);

        retrievedState.Should().Be(newState);
        retrievedState.Status.Should().Be(ProjectStatus.Running);
        retrievedState.CurrentStage.Should().Be("Implementation");
    }
}