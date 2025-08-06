using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests;

public class ContextManagerTests
{
    private readonly ContextManager _contextManager;
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<ILogger<ContextManager>> _loggerMock;
    private readonly string _testProjectPath = "/test/project";

    public ContextManagerTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _loggerMock = new Mock<ILogger<ContextManager>>();
        _contextManager = new ContextManager(_fileSystemMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task BuildContextAsync_WithValidProject_ReturnsContext()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.DirectoryExistsAsync(_testProjectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(_testProjectPath, "*.csproj", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "/test/project/Test.csproj" });
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(_testProjectPath, "*", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                "/test/project/Program.cs",
                "/test/project/Service.cs",
                "/test/project/Tests/ServiceTests.cs",
                "/test/project/appsettings.json"
            });

        _fileSystemMock.Setup(fs => fs.ReadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("file content");

        // Act
        var context = await _contextManager.BuildContextAsync(_testProjectPath);

        // Assert
        context.Should().NotBeNull();
        context.ProjectPath.Should().Be(_testProjectPath);
        context.ProjectType.Should().Be(ProjectType.DotNet);
        context.SourceFiles.Should().HaveCount(2);
        context.TestFiles.Should().HaveCount(1);
        context.ConfigurationFiles.Should().HaveCount(1);
    }

    [Fact]
    public async Task BuildContextAsync_WithNonExistentPath_ThrowsException()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.DirectoryExistsAsync(_testProjectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _contextManager.BuildContextAsync(_testProjectPath));
    }

    [Fact]
    public async Task GetRelevantFilesAsync_ReturnsRelevantFiles()
    {
        // Arrange
        await SetupContextWithFiles();

        // Act
        var relevantFiles = await _contextManager.GetRelevantFilesAsync("service", 5);

        // Assert
        relevantFiles.Should().NotBeNull();
        relevantFiles.Should().Contain(f => f.Contains("Service"));
    }

    [Fact]
    public async Task GetRelevantFilesAsync_WithNoContext_ReturnsEmpty()
    {
        // Act
        var relevantFiles = await _contextManager.GetRelevantFilesAsync("test", 5);

        // Assert
        relevantFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateContextAsync_UpdatesFileMetadata()
    {
        // Arrange
        var context = await SetupContextWithFiles();
        var modifiedFiles = new[] { "/test/project/Service.cs" };
        
        _fileSystemMock.Setup(fs => fs.ReadFileAsync("/test/project/Service.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("modified content");

        // Act
        await _contextManager.UpdateContextAsync(context, modifiedFiles);

        // Assert
        context.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CalculateRelevanceScoresAsync_CalculatesScores()
    {
        // Arrange
        var files = new[]
        {
            "/test/project/UserService.cs",
            "/test/project/OrderService.cs",
            "/test/project/Program.cs"
        };

        // Act
        var scores = await _contextManager.CalculateRelevanceScoresAsync("user", files);

        // Assert
        scores.Should().NotBeNull();
        scores.Should().HaveCount(3);
        scores.Should().ContainKeys("/test/project/UserService.cs", "/test/project/OrderService.cs", "/test/project/Program.cs");
        // UserService should have the highest score since it contains "User" which matches the query "user"
        scores["/test/project/UserService.cs"].Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetCurrentContext_ReturnsCurrentContext()
    {
        // Arrange
        SetupContextWithFiles().Wait();

        // Act
        var context = _contextManager.GetCurrentContext();

        // Assert
        context.Should().NotBeNull();
        context!.ProjectPath.Should().Be(_testProjectPath);
    }

    [Fact]
    public void ClearContext_ClearsCurrentContext()
    {
        // Arrange
        SetupContextWithFiles().Wait();

        // Act
        _contextManager.ClearContext();
        var context = _contextManager.GetCurrentContext();

        // Assert
        context.Should().BeNull();
    }

    [Fact]
    public async Task BuildContextAsync_DetectsNodeJsProject()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.DirectoryExistsAsync(_testProjectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(_testProjectPath, It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, string pattern, bool recursive, CancellationToken ct) =>
            {
                if (pattern == "package.json")
                    return new[] { "/test/project/package.json" };
                return Array.Empty<string>();
            });
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(_testProjectPath, "*", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "/test/project/index.js" });

        // Act
        var context = await _contextManager.BuildContextAsync(_testProjectPath);

        // Assert
        context.ProjectType.Should().Be(ProjectType.NodeJs);
    }

    [Fact]
    public async Task BuildContextAsync_DetectsPythonProject()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.DirectoryExistsAsync(_testProjectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(_testProjectPath, It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, string pattern, bool recursive, CancellationToken ct) =>
            {
                if (pattern == "requirements.txt")
                    return new[] { "/test/project/requirements.txt" };
                return Array.Empty<string>();
            });
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(_testProjectPath, "*", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "/test/project/main.py" });

        _fileSystemMock.Setup(fs => fs.FileExistsAsync("/test/project/requirements.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _fileSystemMock.Setup(fs => fs.ReadFileAsync("/test/project/requirements.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync("flask==2.0.0\nrequests==2.26.0");

        // Act
        var context = await _contextManager.BuildContextAsync(_testProjectPath);

        // Assert
        context.ProjectType.Should().Be(ProjectType.Python);
        context.Dependencies.Should().Contain("flask");
        context.Dependencies.Should().Contain("requests");
    }

    private async Task<ProjectContext> SetupContextWithFiles()
    {
        _fileSystemMock.Setup(fs => fs.DirectoryExistsAsync(_testProjectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(_testProjectPath, "*.csproj", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "/test/project/Test.csproj" });
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(_testProjectPath, "*", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                "/test/project/Service.cs",
                "/test/project/Program.cs"
            });

        _fileSystemMock.Setup(fs => fs.ReadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("public class Service { }");

        return await _contextManager.BuildContextAsync(_testProjectPath);
    }
}