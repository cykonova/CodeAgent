using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests.Services;

public class InternalToolServiceTests
{
    private readonly Mock<IFileSystemService> _fileSystemServiceMock;
    private readonly Mock<IPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<InternalToolService>> _loggerMock;
    private readonly InternalToolService _sut;

    public InternalToolServiceTests()
    {
        _fileSystemServiceMock = new Mock<IFileSystemService>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _loggerMock = new Mock<ILogger<InternalToolService>>();
        _sut = new InternalToolService(
            _fileSystemServiceMock.Object,
            _permissionServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GetAvailableTools_ShouldReturnAllFileOperationTools()
    {
        // Act
        var tools = _sut.GetAvailableTools();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().Contain(t => t.Name == "read_file");
        tools.Should().Contain(t => t.Name == "write_file");
        tools.Should().Contain(t => t.Name == "list_files");
        tools.Should().Contain(t => t.Name == "create_directory");
        tools.Should().Contain(t => t.Name == "delete_file");
        tools.Should().Contain(t => t.Name == "file_exists");
        tools.Should().Contain(t => t.Name == "get_current_directory");
    }

    [Fact]
    public async Task ExecuteToolAsync_ReadFile_WhenPermissionDenied_ShouldReturnError()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test-id",
            Name = "read_file",
            Arguments = new Dictionary<string, object> { ["path"] = "test.txt" }
        };
        
        _permissionServiceMock.Setup(x => x.GetSafePath(It.IsAny<string>()))
            .Returns("/safe/path/test.txt");
        _permissionServiceMock.Setup(x => x.RequestPermissionAsync(
                "Read file", 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ExecuteToolAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Permission denied");
        result.ToolCallId.Should().Be("test-id");
        
        // Verify file was not read
        _fileSystemServiceMock.Verify(x => x.ReadFileAsync(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteToolAsync_ReadFile_WhenPermissionGranted_ShouldReadFile()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test-id",
            Name = "read_file",
            Arguments = new Dictionary<string, object> { ["path"] = "test.txt" }
        };
        
        _permissionServiceMock.Setup(x => x.GetSafePath(It.IsAny<string>()))
            .Returns("/safe/path/test.txt");
        _permissionServiceMock.Setup(x => x.RequestPermissionAsync(
                "Read file", 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(true);
        _fileSystemServiceMock.Setup(x => x.FileExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("file content");

        // Act
        var result = await _sut.ExecuteToolAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().Be("file content");
        result.ToolCallId.Should().Be("test-id");
        
        _fileSystemServiceMock.Verify(x => x.ReadFileAsync(
            "/safe/path/test.txt", 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_WriteFile_WhenPermissionDenied_ShouldReturnError()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test-id",
            Name = "write_file",
            Arguments = new Dictionary<string, object> 
            { 
                ["path"] = "test.txt",
                ["content"] = "new content"
            }
        };
        
        _permissionServiceMock.Setup(x => x.GetSafePath(It.IsAny<string>()))
            .Returns("/safe/path/test.txt");
        _permissionServiceMock.Setup(x => x.RequestPermissionAsync(
                "Write file", 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ExecuteToolAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Permission denied");
        
        // Verify file was not written
        _fileSystemServiceMock.Verify(x => x.WriteFileAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteToolAsync_WriteFile_WhenPermissionGranted_ShouldWriteFile()
    {
        // Arrange
        var content = "new content";
        var toolCall = new ToolCall
        {
            Id = "test-id",
            Name = "write_file",
            Arguments = new Dictionary<string, object> 
            { 
                ["path"] = "test.txt",
                ["content"] = content
            }
        };
        
        _permissionServiceMock.Setup(x => x.GetSafePath(It.IsAny<string>()))
            .Returns("/safe/path/test.txt");
        _permissionServiceMock.Setup(x => x.RequestPermissionAsync(
                "Write file", 
                "/safe/path/test.txt", 
                $"Write {content.Length} characters"))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ExecuteToolAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("Successfully wrote");
        result.Content.Should().Contain("11 characters");
        
        _fileSystemServiceMock.Verify(x => x.WriteFileAsync(
            "/safe/path/test.txt",
            content,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_DeleteFile_WhenPermissionDenied_ShouldReturnError()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test-id",
            Name = "delete_file",
            Arguments = new Dictionary<string, object> { ["path"] = "test.txt" }
        };
        
        _permissionServiceMock.Setup(x => x.GetSafePath(It.IsAny<string>()))
            .Returns("/safe/path/test.txt");
        _permissionServiceMock.Setup(x => x.RequestPermissionAsync(
                "Delete file", 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ExecuteToolAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Permission denied");
        
        // Verify file was not deleted
        _fileSystemServiceMock.Verify(x => x.DeleteFileAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteToolAsync_CreateDirectory_WhenPermissionDenied_ShouldReturnError()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test-id",
            Name = "create_directory",
            Arguments = new Dictionary<string, object> { ["path"] = "new_dir" }
        };
        
        _permissionServiceMock.Setup(x => x.GetSafePath(It.IsAny<string>()))
            .Returns("/safe/path/new_dir");
        _permissionServiceMock.Setup(x => x.RequestPermissionAsync(
                "Create directory", 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ExecuteToolAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Permission denied");
        
        // Verify directory was not created
        _fileSystemServiceMock.Verify(x => x.CreateDirectoryAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_ShouldReturnError()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test-id",
            Name = "unknown_tool",
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = await _sut.ExecuteToolAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Unknown tool: unknown_tool");
    }

    [Fact]
    public async Task ExecuteToolAsync_ListFiles_ShouldNotRequireExplicitPermission()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "test-id",
            Name = "list_files",
            Arguments = new Dictionary<string, object>()
        };
        
        _fileSystemServiceMock.Setup(x => x.GetFilesAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "file1.txt", "file2.txt" });
        _fileSystemServiceMock.Setup(x => x.GetDirectoriesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "dir1", "dir2" });

        // Act
        var result = await _sut.ExecuteToolAsync(toolCall);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("file1.txt");
        result.Content.Should().Contain("file2.txt");
        result.Content.Should().Contain("dir1");
        result.Content.Should().Contain("dir2");
        
        // Verify permission was not requested for listing files
        _permissionServiceMock.Verify(x => x.RequestPermissionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>()), Times.Never);
    }
}