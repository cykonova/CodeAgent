using CodeAgent.Agents;
using CodeAgent.Agents.Contracts;
using CodeAgent.Agents.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Agents.Tests;

public class AgentOrchestratorTests
{
    private readonly Mock<IAgentSetupService> _setupServiceMock;
    private readonly Mock<IWorkflowEngine> _workflowEngineMock;
    private readonly Mock<ILogger<AgentOrchestrator>> _loggerMock;
    private readonly AgentOrchestrator _orchestrator;

    public AgentOrchestratorTests()
    {
        _setupServiceMock = new Mock<IAgentSetupService>();
        _workflowEngineMock = new Mock<IWorkflowEngine>();
        _loggerMock = new Mock<ILogger<AgentOrchestrator>>();
        _orchestrator = new AgentOrchestrator(_setupServiceMock.Object, _workflowEngineMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetupAgents_WhenCalled()
    {
        // Arrange
        var mockAgents = new List<IAgent>
        {
            CreateMockAgent("agent1", AgentType.Planning),
            CreateMockAgent("agent2", AgentType.Coding)
        };
        
        _setupServiceMock.Setup(x => x.SetupAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAgents);

        // Act
        var result = await _orchestrator.InitializeAsync();

        // Assert
        result.Should().BeTrue();
        _orchestrator.GetAllAgents().Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessCommandAsync_ShouldExecuteWorkflow_WhenCommandProvided()
    {
        // Arrange
        await InitializeOrchestratorWithAgents();
        
        var request = new OrchestratorRequest
        {
            Command = "implement feature",
            Content = "Add user authentication",
            SessionId = "session123"
        };

        var workflow = new WorkflowDefinition
        {
            Id = "implementation",
            Name = "Implementation Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "Planning",
                    AgentType = AgentType.Planning,
                    Command = "analyze_requirements",
                    IsRequired = true
                }
            }
        };

        _workflowEngineMock.Setup(x => x.DetermineWorkflowAsync(It.IsAny<OrchestratorRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        // Act
        var response = await _orchestrator.ProcessCommandAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.AgentResponses.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAgentForType_ShouldReturnCorrectAgent_WhenTypeExists()
    {
        // Arrange
        await InitializeOrchestratorWithAgents();

        // Act
        var agent = _orchestrator.GetAgentForType(AgentType.Planning);

        // Assert
        agent.Should().NotBeNull();
        agent!.Type.Should().Be(AgentType.Planning);
    }

    [Fact]
    public async Task GetAgentForType_ShouldReturnNull_WhenTypeDoesNotExist()
    {
        // Arrange
        await InitializeOrchestratorWithAgents();

        // Act
        var agent = _orchestrator.GetAgentForType(AgentType.Custom);

        // Assert
        agent.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_ShouldReturnError_WhenWorkflowNotFound()
    {
        // Arrange
        _workflowEngineMock.Setup(x => x.GetWorkflowAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDefinition?)null);

        var request = new WorkflowRequest
        {
            Content = "Test content"
        };

        // Act
        var response = await _orchestrator.ExecuteWorkflowAsync("nonexistent", request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ShutdownAsync_ShouldShutdownAllAgents_WhenCalled()
    {
        // Arrange
        var mockAgent1 = new Mock<IAgent>();
        var mockAgent2 = new Mock<IAgent>();
        
        mockAgent1.Setup(x => x.AgentId).Returns("agent1");
        mockAgent1.Setup(x => x.Type).Returns(AgentType.Planning);
        mockAgent2.Setup(x => x.AgentId).Returns("agent2");
        mockAgent2.Setup(x => x.Type).Returns(AgentType.Coding);

        var mockAgents = new List<IAgent> { mockAgent1.Object, mockAgent2.Object };
        
        _setupServiceMock.Setup(x => x.SetupAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAgents);

        await _orchestrator.InitializeAsync();

        // Act
        await _orchestrator.ShutdownAsync();

        // Assert
        mockAgent1.Verify(x => x.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockAgent2.Verify(x => x.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orchestrator.GetAllAgents().Should().BeEmpty();
    }

    private async Task InitializeOrchestratorWithAgents()
    {
        var mockAgents = new List<IAgent>
        {
            CreateMockAgent("planning-agent", AgentType.Planning),
            CreateMockAgent("coding-agent", AgentType.Coding)
        };
        
        _setupServiceMock.Setup(x => x.SetupAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAgents);

        await _orchestrator.InitializeAsync();
    }

    private IAgent CreateMockAgent(string agentId, AgentType type)
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(x => x.AgentId).Returns(agentId);
        mockAgent.Setup(x => x.Type).Returns(type);
        mockAgent.Setup(x => x.Name).Returns($"{type} Agent");
        
        mockAgent.Setup(x => x.ExecuteAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResponse
            {
                AgentId = agentId,
                Success = true,
                Content = $"Response from {agentId}",
                UpdatedContext = new AgentContext()
            });

        return mockAgent.Object;
    }
}