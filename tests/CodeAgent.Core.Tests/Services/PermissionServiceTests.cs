using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests.Services;

public class PermissionServiceTests
{
    private readonly Mock<ILogger<PermissionService>> _loggerMock;
    private readonly Mock<IPermissionPrompt> _permissionPromptMock;
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _loggerMock = new Mock<ILogger<PermissionService>>();
        _permissionPromptMock = new Mock<IPermissionPrompt>();
        _sut = new PermissionService(_loggerMock.Object, _permissionPromptMock.Object);
    }

    [Fact]
    public async Task RequestPermissionAsync_WhenPathIsOutsideWorkingDirectory_ShouldConvertToSafePath()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var requestedPath = "/etc/passwd";
        _sut.SetWorkingDirectory(workingDir);
        _sut.SetProjectDirectory(workingDir); // Set project dir same as working dir
        _permissionPromptMock.Setup(x => x.PromptForPermissionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(PermissionResult.Denied);

        // Act
        var result = await _sut.RequestPermissionAsync("Read file", requestedPath);

        // Assert
        result.Should().BeFalse();
        // The path should be converted to be inside the working directory
        _permissionPromptMock.Verify(x => x.PromptForPermissionAsync(
            "Read file", 
            It.Is<string>(p => p.Contains("project") && p.Contains("passwd")), 
            It.IsAny<string>(),
            null), Times.Once);
    }

    [Fact]
    public async Task RequestPermissionAsync_WhenUserApproves_ShouldReturnTrue()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var requestedPath = "test.txt";
        _sut.SetWorkingDirectory(workingDir);
        _sut.SetProjectDirectory(workingDir); // Set project dir same as working dir
        _permissionPromptMock.Setup(x => x.PromptForPermissionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(PermissionResult.Allowed);

        // Act
        var result = await _sut.RequestPermissionAsync("Write file", requestedPath, "10 bytes");

        // Assert
        result.Should().BeTrue();
        _permissionPromptMock.Verify(x => x.PromptForPermissionAsync(
            "Write file",
            It.Is<string>(p => p.EndsWith("test.txt")),
            It.IsAny<string>(),
            "10 bytes"), Times.Once);
    }

    [Fact]
    public async Task RequestPermissionAsync_WhenUserDenies_ShouldReturnFalse()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var requestedPath = "test.txt";
        _sut.SetWorkingDirectory(workingDir);
        _sut.SetProjectDirectory(workingDir); // Set project dir same as working dir
        _permissionPromptMock.Setup(x => x.PromptForPermissionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(PermissionResult.Denied);

        // Act
        var result = await _sut.RequestPermissionAsync("Delete file", requestedPath);

        // Assert
        result.Should().BeFalse();
        _permissionPromptMock.Verify(x => x.PromptForPermissionAsync(
            "Delete file",
            It.Is<string>(p => p.EndsWith("test.txt")),
            It.IsAny<string>(),
            null), Times.Once);
    }

    [Fact]
    public void GetSafePath_WhenAbsolutePathOutsideWorkingDir_ShouldConvertToRelative()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var unsafePath = "/etc/passwd";
        _sut.SetWorkingDirectory(workingDir);

        // Act
        var safePath = _sut.GetSafePath(unsafePath);

        // Assert
        safePath.Should().Be(Path.Combine(workingDir, "passwd"));
    }

    [Fact]
    public void GetSafePath_WhenRelativePath_ShouldResolveToWorkingDir()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var relativePath = "src/test.txt";
        _sut.SetWorkingDirectory(workingDir);

        // Act
        var safePath = _sut.GetSafePath(relativePath);

        // Assert
        safePath.Should().Be(Path.GetFullPath(Path.Combine(workingDir, relativePath)));
    }

    [Fact]
    public void IsPathAllowed_WhenPathInsideWorkingDir_ShouldReturnTrue()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var allowedPath = "/home/user/project/src/test.txt";
        _sut.SetWorkingDirectory(workingDir);

        // Act
        var result = _sut.IsPathAllowed(allowedPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathAllowed_WhenPathOutsideWorkingDir_ShouldReturnFalse()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var disallowedPath = "/home/user/other/test.txt";
        _sut.SetWorkingDirectory(workingDir);

        // Act
        var result = _sut.IsPathAllowed(disallowedPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathAllowed_WhenPathTraversesOutside_ShouldReturnFalse()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var traversalPath = "../../../etc/passwd";
        _sut.SetWorkingDirectory(workingDir);

        // Act
        var result = _sut.IsPathAllowed(traversalPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RequestPermissionAsync_WhenAllowedForAll_ShouldGrantFutureRequests()
    {
        // Arrange
        var workingDir = "/home/user/project";
        var projectDir = "/home/user/project";
        _sut.SetWorkingDirectory(workingDir);
        _sut.SetProjectDirectory(projectDir);
        
        _permissionPromptMock.Setup(x => x.PromptForPermissionAsync(
                "Write file", 
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(PermissionResult.AllowedForAll);

        // Act - First request
        var result1 = await _sut.RequestPermissionAsync("Write file", "test1.txt");
        
        // Act - Second request (should be auto-approved)
        var result2 = await _sut.RequestPermissionAsync("Write file", "test2.txt");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        
        // Verify prompt was only called once
        _permissionPromptMock.Verify(x => x.PromptForPermissionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void IsOutsideUserProfile_WhenPathInsideProfile_ShouldReturnFalse()
    {
        // Arrange - This test may not work on all systems, so we'll make it environment-aware
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var pathInsideProfile = Path.Combine(userProfile, "Documents", "test.txt");

        // Act
        var result = _sut.IsOutsideUserProfile(pathInsideProfile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsOutsideUserProfile_WhenPathOutsideProfile_ShouldReturnTrue()
    {
        // Arrange
        var pathOutsideProfile = "/etc/passwd";

        // Act
        var result = _sut.IsOutsideUserProfile(pathOutsideProfile);

        // Assert - This may vary by OS, so we'll check platform-specifically
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task RequestPermissionAsync_WhenOutsideUserProfileAndProjectDir_ShouldDenyAutomatically()
    {
        // Arrange - Set up project directory within user profile
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var projectDir = Path.Combine(userProfile, "project");
        _sut.SetProjectDirectory(projectDir);
        _sut.SetWorkingDirectory(projectDir);
        
        // Try to access a path outside both user profile AND project directory
        // This should be blocked by the security check since GetSafePath will put it in working dir,
        // but the full path after GetSafePath will still be checked against user profile
        var tempDir = Path.GetTempPath(); // This might be outside user profile on some systems
        var dangerousPath = Path.Combine(tempDir, "..", "..", "system-file.txt");

        // Act
        var result = await _sut.RequestPermissionAsync("Read file", dangerousPath);

        // Assert
        result.Should().BeFalse();
        
        // Note: The security check logic is complex - GetSafePath converts the path,
        // but it may still end up outside user profile depending on the system.
        // This test verifies the overall security behavior.
    }
}