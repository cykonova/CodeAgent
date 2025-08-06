using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Teams;
using CodeAgent.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Web.Tests;

public class TeamServiceTests
{
    private readonly TeamService _teamService;
    private readonly Mock<ILogger<TeamService>> _loggerMock;

    public TeamServiceTests()
    {
        _loggerMock = new Mock<ILogger<TeamService>>();
        _teamService = new TeamService(_loggerMock.Object);
    }

    [Fact]
    public async Task CreateTeamAsync_CreatesNewTeam()
    {
        // Arrange
        var teamName = "Test Team";
        var description = "Test Description";
        var ownerId = "owner-123";

        // Act
        var team = await _teamService.CreateTeamAsync(teamName, description, ownerId);

        // Assert
        team.Should().NotBeNull();
        team.Name.Should().Be(teamName);
        team.Description.Should().Be(description);
        team.Members.Should().ContainSingle(m => m.UserId == ownerId && m.Role == TeamRole.Owner);
    }

    [Fact]
    public async Task AddMemberAsync_AddsMemberToTeam()
    {
        // Arrange
        var team = await _teamService.CreateTeamAsync("Test Team", "Description", "owner-123");
        var newUserId = "user-456";

        // Act
        await _teamService.AddMemberAsync(team.Id, newUserId, TeamRole.Member);
        var updatedTeam = await _teamService.GetTeamAsync(team.Id);

        // Assert
        updatedTeam!.Members.Should().HaveCount(2);
        updatedTeam.Members.Should().Contain(m => m.UserId == newUserId && m.Role == TeamRole.Member);
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesMemberFromTeam()
    {
        // Arrange
        var team = await _teamService.CreateTeamAsync("Test Team", "Description", "owner-123");
        await _teamService.AddMemberAsync(team.Id, "user-456", TeamRole.Member);

        // Act
        await _teamService.RemoveMemberAsync(team.Id, "user-456");
        var updatedTeam = await _teamService.GetTeamAsync(team.Id);

        // Assert
        updatedTeam!.Members.Should().HaveCount(1);
        updatedTeam.Members.Should().NotContain(m => m.UserId == "user-456");
    }

    [Fact]
    public async Task ShareAgentAsync_SharesAgentWithTeam()
    {
        // Arrange
        var team = await _teamService.CreateTeamAsync("Test Team", "Description", "owner-123");
        var agentConfig = new SharedAgentConfig
        {
            Name = "Test Agent",
            Provider = "OpenAI",
            Configuration = new Dictionary<string, object> { ["model"] = "gpt-4" }
        };

        // Act
        await _teamService.ShareAgentAsync(team.Id, agentConfig, "owner-123");
        var sharedAgents = await _teamService.GetSharedAgentsAsync(team.Id);

        // Assert
        sharedAgents.Should().ContainSingle();
        sharedAgents.First().Name.Should().Be("Test Agent");
        sharedAgents.First().SharedBy.Should().Be("owner-123");
    }

    [Fact]
    public async Task UnshareAgentAsync_RemovesSharedAgent()
    {
        // Arrange
        var team = await _teamService.CreateTeamAsync("Test Team", "Description", "owner-123");
        var agentConfig = new SharedAgentConfig
        {
            Name = "Test Agent",
            Provider = "OpenAI",
            Configuration = new Dictionary<string, object>()
        };
        await _teamService.ShareAgentAsync(team.Id, agentConfig, "owner-123");
        var sharedAgents = await _teamService.GetSharedAgentsAsync(team.Id);
        var agentId = sharedAgents.First().Id;

        // Act
        await _teamService.UnshareAgentAsync(team.Id, agentId);
        var updatedAgents = await _teamService.GetSharedAgentsAsync(team.Id);

        // Assert
        updatedAgents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTeamsForUserAsync_ReturnsUserTeams()
    {
        // Arrange
        var userId = "user-123";
        var team1 = await _teamService.CreateTeamAsync("Team 1", "Description 1", userId);
        var team2 = await _teamService.CreateTeamAsync("Team 2", "Description 2", "other-user");
        await _teamService.AddMemberAsync(team2.Id, userId, TeamRole.Member);

        // Act
        var teams = await _teamService.GetTeamsForUserAsync(userId);

        // Assert
        teams.Should().HaveCount(2);
        teams.Should().Contain(t => t.Id == team1.Id);
        teams.Should().Contain(t => t.Id == team2.Id);
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_UpdatesMemberRole()
    {
        // Arrange
        var team = await _teamService.CreateTeamAsync("Test Team", "Description", "owner-123");
        await _teamService.AddMemberAsync(team.Id, "user-456", TeamRole.Member);

        // Act
        await _teamService.UpdateMemberRoleAsync(team.Id, "user-456", TeamRole.Admin);
        var updatedTeam = await _teamService.GetTeamAsync(team.Id);

        // Assert
        var member = updatedTeam!.Members.First(m => m.UserId == "user-456");
        member.Role.Should().Be(TeamRole.Admin);
    }

    [Fact]
    public async Task DeleteTeamAsync_DeletesTeam()
    {
        // Arrange
        var team = await _teamService.CreateTeamAsync("Test Team", "Description", "owner-123");

        // Act
        await _teamService.DeleteTeamAsync(team.Id);
        var deletedTeam = await _teamService.GetTeamAsync(team.Id);

        // Assert
        deletedTeam.Should().BeNull();
    }

    [Fact]
    public async Task CannotRemoveLastOwner_ThrowsException()
    {
        // Arrange
        var team = await _teamService.CreateTeamAsync("Test Team", "Description", "owner-123");

        // Act & Assert
        var act = async () => await _teamService.RemoveMemberAsync(team.Id, "owner-123");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot remove the last owner from a team");
    }

    [Fact]
    public async Task ShareAgentAsync_PreventsDuplicateNames()
    {
        // Arrange
        var team = await _teamService.CreateTeamAsync("Test Team", "Description", "owner-123");
        var agentConfig = new SharedAgentConfig
        {
            Name = "Test Agent",
            Provider = "OpenAI",
            Configuration = new Dictionary<string, object>()
        };
        await _teamService.ShareAgentAsync(team.Id, agentConfig, "owner-123");

        // Act & Assert
        var act = async () => await _teamService.ShareAgentAsync(team.Id, agentConfig, "owner-123");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("An agent with this name is already shared with the team");
    }
}