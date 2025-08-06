using CodeAgent.Domain.Models.Security;
using CodeAgent.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Infrastructure.Tests;

public class SecurityServiceTests
{
    private readonly SecurityService _securityService;
    private readonly Mock<ILogger<SecurityService>> _loggerMock;

    public SecurityServiceTests()
    {
        _loggerMock = new Mock<ILogger<SecurityService>>();
        _securityService = new SecurityService(_loggerMock.Object);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsSystemRoles()
    {
        // Act
        var roles = await _securityService.GetRolesAsync();

        // Assert
        roles.Should().NotBeEmpty();
        roles.Should().Contain(r => r.Id == SystemRoles.Administrator);
        roles.Should().Contain(r => r.Id == SystemRoles.Developer);
        roles.Should().Contain(r => r.Id == SystemRoles.Reviewer);
        roles.Should().Contain(r => r.Id == SystemRoles.ReadOnly);
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidRole_CreatesRole()
    {
        // Arrange
        var role = new Role
        {
            Id = "custom-role",
            Name = "Custom Role",
            Description = "Test custom role",
            Permissions = new List<Permission>
            {
                new() { Name = SystemPermissions.FileRead, Resource = "*", Action = "read" }
            }
        };

        // Act
        var createdRole = await _securityService.CreateRoleAsync(role);

        // Assert
        createdRole.Should().NotBeNull();
        createdRole.Id.Should().Be(role.Id);
        
        var retrievedRole = await _securityService.GetRoleAsync(role.Id);
        retrievedRole.Should().NotBeNull();
        retrievedRole!.Name.Should().Be(role.Name);
    }

    [Fact]
    public async Task CreateRoleAsync_WithDuplicateId_ThrowsException()
    {
        // Arrange
        var role = new Role { Id = "duplicate", Name = "Role 1" };
        await _securityService.CreateRoleAsync(role);

        var duplicateRole = new Role { Id = "duplicate", Name = "Role 2" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _securityService.CreateRoleAsync(duplicateRole));
    }

    [Fact]
    public async Task DeleteRoleAsync_WithSystemRole_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _securityService.DeleteRoleAsync(SystemRoles.Administrator));
    }

    [Fact]
    public async Task AssignRoleAsync_AssignsRoleToUser()
    {
        // Arrange
        var userId = "test-user";
        var roleId = SystemRoles.Developer;

        // Act
        await _securityService.AssignRoleAsync(userId, roleId);
        var userRoles = await _securityService.GetUserRolesAsync(userId);

        // Assert
        userRoles.Should().Contain(r => r.Id == roleId);
    }

    [Fact]
    public async Task RemoveRoleAsync_RemovesRoleFromUser()
    {
        // Arrange
        var userId = "test-user";
        var roleId = SystemRoles.Developer;
        await _securityService.AssignRoleAsync(userId, roleId);

        // Act
        await _securityService.RemoveRoleAsync(userId, roleId);
        var userRoles = await _securityService.GetUserRolesAsync(userId);

        // Assert
        userRoles.Should().NotContain(r => r.Id == roleId);
    }

    [Fact]
    public async Task HasPermissionAsync_WithAssignedRole_ReturnsTrue()
    {
        // Arrange
        var userId = "test-user";
        await _securityService.AssignRoleAsync(userId, SystemRoles.Developer);

        // Act
        var hasPermission = await _securityService.HasPermissionAsync(userId, SystemPermissions.FileRead);

        // Assert
        hasPermission.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WithoutRole_ReturnsFalse()
    {
        // Act
        var hasPermission = await _securityService.HasPermissionAsync("unknown-user", SystemPermissions.FileWrite);

        // Assert
        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSessionAsync_CreatesValidSession()
    {
        // Arrange
        var userId = "test-user";

        // Act
        var session = await _securityService.CreateSessionAsync(userId);

        // Assert
        session.Should().NotBeNull();
        session.UserId.Should().Be(userId);
        session.Token.Should().NotBeNullOrEmpty();
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateSessionAsync_WithValidSession_ReturnsTrue()
    {
        // Arrange
        var userId = "test-user";
        var session = await _securityService.CreateSessionAsync(userId);

        // Act
        var isValid = await _securityService.ValidateSessionAsync(session.Token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSessionAsync_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var isValid = await _securityService.ValidateSessionAsync("invalid-token");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateSessionAsync_InvalidatesSession()
    {
        // Arrange
        var userId = "test-user";
        var session = await _securityService.CreateSessionAsync(userId);

        // Act
        await _securityService.InvalidateSessionAsync(session.Token);
        var isValid = await _securityService.ValidateSessionAsync(session.Token);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePolicyAsync_CreatesPolicy()
    {
        // Arrange
        var policy = new SecurityPolicy
        {
            Id = "test-policy",
            Name = "Test Policy",
            Type = PolicyType.Access,
            Enforcement = PolicyEnforcement.Warning
        };

        // Act
        var createdPolicy = await _securityService.CreatePolicyAsync(policy);

        // Assert
        createdPolicy.Should().NotBeNull();
        createdPolicy.Id.Should().Be(policy.Id);
        
        var retrievedPolicy = await _securityService.GetPolicyAsync(policy.Id);
        retrievedPolicy.Should().NotBeNull();
    }

    [Fact]
    public async Task EnableMfaAsync_EnablesMfaForUser()
    {
        // Arrange
        var userId = "test-user";
        var secret = "test-secret";

        // Act
        await _securityService.EnableMfaAsync(userId, secret);
        var isMfaEnabled = await _securityService.IsMfaEnabledAsync(userId);

        // Assert
        isMfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task DisableMfaAsync_DisablesMfaForUser()
    {
        // Arrange
        var userId = "test-user";
        await _securityService.EnableMfaAsync(userId, "secret");

        // Act
        await _securityService.DisableMfaAsync(userId);
        var isMfaEnabled = await _securityService.IsMfaEnabledAsync(userId);

        // Assert
        isMfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateMfaTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var userId = "test-user";
        var token = "123456";

        // Act
        var isValid = await _securityService.ValidateMfaTokenAsync(userId, token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateMfaTokenAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var userId = "test-user";
        var token = "invalid";

        // Act
        var isValid = await _securityService.ValidateMfaTokenAsync(userId, token);

        // Assert
        isValid.Should().BeFalse();
    }
}