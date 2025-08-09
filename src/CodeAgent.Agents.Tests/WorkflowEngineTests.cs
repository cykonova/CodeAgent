using CodeAgent.Agents;
using CodeAgent.Agents.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Agents.Tests;

public class WorkflowEngineTests
{
    private readonly Mock<ILogger<WorkflowEngine>> _loggerMock;
    private readonly WorkflowEngine _engine;

    public WorkflowEngineTests()
    {
        _loggerMock = new Mock<ILogger<WorkflowEngine>>();
        _engine = new WorkflowEngine(_loggerMock.Object);
    }

    [Theory]
    [InlineData("implement new feature")]
    [InlineData("create user authentication")]
    [InlineData("build login system")]
    [InlineData("add feature")]
    public async Task DetermineWorkflowAsync_ShouldReturnImplementationWorkflow_ForImplementationCommands(string command)
    {
        // Arrange
        var request = new OrchestratorRequest { Command = command };

        // Act
        var workflow = await _engine.DetermineWorkflowAsync(request);

        // Assert
        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("implementation");
        workflow.Steps.Should().NotBeEmpty();
        workflow.Steps.Should().Contain(s => s.AgentType == AgentType.Planning);
        workflow.Steps.Should().Contain(s => s.AgentType == AgentType.Coding);
        workflow.Steps.Should().Contain(s => s.AgentType == AgentType.Testing);
    }

    [Theory]
    [InlineData("review code")]
    [InlineData("analyze security")]
    [InlineData("check quality")]
    [InlineData("audit codebase")]
    public async Task DetermineWorkflowAsync_ShouldReturnReviewWorkflow_ForReviewCommands(string command)
    {
        // Arrange
        var request = new OrchestratorRequest { Command = command };

        // Act
        var workflow = await _engine.DetermineWorkflowAsync(request);

        // Assert
        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("review");
        workflow.Steps.Should().NotBeEmpty();
        workflow.Steps.Should().Contain(s => s.Command == "analyze_code");
    }

    [Theory]
    [InlineData("test feature")]
    [InlineData("verify functionality")]
    [InlineData("validate changes")]
    public async Task DetermineWorkflowAsync_ShouldReturnTestingWorkflow_ForTestingCommands(string command)
    {
        // Arrange
        var request = new OrchestratorRequest { Command = command };

        // Act
        var workflow = await _engine.DetermineWorkflowAsync(request);

        // Assert
        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("testing");
        workflow.Steps.Should().NotBeEmpty();
        workflow.Steps.Should().Contain(s => s.AgentType == AgentType.Testing);
    }

    [Theory]
    [InlineData("document api")]
    [InlineData("explain function")]
    [InlineData("describe architecture")]
    public async Task DetermineWorkflowAsync_ShouldReturnDocumentationWorkflow_ForDocumentationCommands(string command)
    {
        // Arrange
        var request = new OrchestratorRequest { Command = command };

        // Act
        var workflow = await _engine.DetermineWorkflowAsync(request);

        // Assert
        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("documentation");
        workflow.Steps.Should().NotBeEmpty();
        workflow.Steps.Should().Contain(s => s.AgentType == AgentType.Documentation);
    }

    [Theory]
    [InlineData("refactor code")]
    [InlineData("optimize performance")]
    [InlineData("improve structure")]
    public async Task DetermineWorkflowAsync_ShouldReturnRefactoringWorkflow_ForRefactoringCommands(string command)
    {
        // Arrange
        var request = new OrchestratorRequest { Command = command };

        // Act
        var workflow = await _engine.DetermineWorkflowAsync(request);

        // Assert
        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("refactoring");
        workflow.Steps.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DetermineWorkflowAsync_ShouldReturnDefaultWorkflow_ForUnknownCommands()
    {
        // Arrange
        var request = new OrchestratorRequest { Command = "random command" };

        // Act
        var workflow = await _engine.DetermineWorkflowAsync(request);

        // Assert
        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("default");
        workflow.Steps.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWorkflowAsync_ShouldReturnWorkflow_WhenWorkflowExists()
    {
        // Arrange & Act
        var workflow = await _engine.GetWorkflowAsync("implementation");

        // Assert
        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("implementation");
        workflow.Name.Should().Be("Implementation Workflow");
    }

    [Fact]
    public async Task GetWorkflowAsync_ShouldReturnNull_WhenWorkflowDoesNotExist()
    {
        // Arrange & Act
        var workflow = await _engine.GetWorkflowAsync("nonexistent");

        // Assert
        workflow.Should().BeNull();
    }

    [Fact]
    public async Task RegisterWorkflowAsync_ShouldAddNewWorkflow()
    {
        // Arrange
        var customWorkflow = new WorkflowDefinition
        {
            Id = "custom",
            Name = "Custom Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", AgentType = AgentType.Coding }
            }
        };

        // Act
        await _engine.RegisterWorkflowAsync(customWorkflow);
        var retrievedWorkflow = await _engine.GetWorkflowAsync("custom");

        // Assert
        retrievedWorkflow.Should().NotBeNull();
        retrievedWorkflow!.Id.Should().Be("custom");
        retrievedWorkflow.Name.Should().Be("Custom Workflow");
    }

    [Fact]
    public async Task WorkflowSteps_ShouldHaveCorrectOrder()
    {
        // Arrange
        var request = new OrchestratorRequest { Command = "implement feature" };

        // Act
        var workflow = await _engine.DetermineWorkflowAsync(request);

        // Assert
        workflow.Should().NotBeNull();
        var steps = workflow!.Steps.OrderBy(s => s.Order).ToList();
        
        steps[0].Name.Should().Be("Planning");
        steps[0].Order.Should().Be(1);
        
        steps[1].Name.Should().Be("Architecture Design");
        steps[1].Order.Should().Be(2);
        
        steps[2].Name.Should().Be("Implementation");
        steps[2].Order.Should().Be(3);
    }
}