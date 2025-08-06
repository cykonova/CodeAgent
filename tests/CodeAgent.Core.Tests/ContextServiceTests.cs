using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodeAgent.Core.Tests;

public class ContextServiceTests
{
    private readonly Mock<IFileSystemService> _fileSystemServiceMock;
    private readonly ContextService _contextService;

    public ContextServiceTests()
    {
        _fileSystemServiceMock = new Mock<IFileSystemService>();
        _contextService = new ContextService(_fileSystemServiceMock.Object);
    }

    [Fact]
    public async Task AddFileToContextAsync_AddsFileSuccessfully()
    {
        // Arrange
        var filePath = "test.txt";
        var content = "Test content";
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(filePath, default))
            .ReturnsAsync(content);

        // Act
        await _contextService.AddFileToContextAsync(filePath);
        var files = await _contextService.GetContextFilesAsync();

        // Assert
        files.Should().Contain(filePath);
    }

    [Fact]
    public async Task AddFileToContextAsync_DoesNotAddDuplicates()
    {
        // Arrange
        var filePath = "test.txt";
        var content = "Test content";
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(filePath, default))
            .ReturnsAsync(content);

        // Act
        await _contextService.AddFileToContextAsync(filePath);
        await _contextService.AddFileToContextAsync(filePath);
        var files = await _contextService.GetContextFilesAsync();

        // Assert
        files.Should().ContainSingle(f => f == filePath);
    }

    [Fact]
    public async Task RemoveFileFromContextAsync_RemovesFileSuccessfully()
    {
        // Arrange
        var filePath = "test.txt";
        var content = "Test content";
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(filePath, default))
            .ReturnsAsync(content);
        await _contextService.AddFileToContextAsync(filePath);

        // Act
        await _contextService.RemoveFileFromContextAsync(filePath);
        var files = await _contextService.GetContextFilesAsync();

        // Assert
        files.Should().NotContain(filePath);
    }

    [Fact]
    public async Task ClearContextAsync_RemovesAllFiles()
    {
        // Arrange
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(It.IsAny<string>(), default))
            .ReturnsAsync("content");
        await _contextService.AddFileToContextAsync("file1.txt");
        await _contextService.AddFileToContextAsync("file2.txt");

        // Act
        await _contextService.ClearContextAsync();
        var files = await _contextService.GetContextFilesAsync();

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task GetContextSummaryAsync_ReturnsDetailedSummary()
    {
        // Arrange
        var filePath = "test.txt";
        var content = "Line 1\nLine 2\nLine 3";
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(filePath, default))
            .ReturnsAsync(content);
        await _contextService.AddFileToContextAsync(filePath);

        // Act
        var summary = await _contextService.GetContextSummaryAsync();

        // Assert
        summary.Should().Contain("1 files");
        summary.Should().Contain(filePath);
        summary.Should().Contain("3 lines");
    }

    [Fact]
    public async Task BuildPromptContextAsync_IncludesBasePromptAndFiles()
    {
        // Arrange
        var basePrompt = "Analyze this code:";
        var filePath = "test.txt";
        var content = "function test() { }";
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(filePath, default))
            .ReturnsAsync(content);
        await _contextService.AddFileToContextAsync(filePath);

        // Act
        var prompt = await _contextService.BuildPromptContextAsync(basePrompt);

        // Assert
        prompt.Should().Contain(basePrompt);
        prompt.Should().Contain(filePath);
        prompt.Should().Contain(content);
        prompt.Should().Contain("CONTEXT FILES");
    }

    [Fact]
    public async Task BuildPromptContextAsync_TruncatesLargeFiles()
    {
        // Arrange
        var basePrompt = "Analyze:";
        var filePath = "large.txt";
        var content = new string('x', 5000); // Large content
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(filePath, default))
            .ReturnsAsync(content);
        await _contextService.AddFileToContextAsync(filePath);

        // Act
        var prompt = await _contextService.BuildPromptContextAsync(basePrompt);

        // Assert
        prompt.Should().Contain("truncated");
        prompt.Should().Contain("remaining");
    }

    [Fact]
    public async Task SaveContextAsync_SavesContextToFile()
    {
        // Arrange
        var contextName = "test-context";
        var filePath = "test.txt";
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(filePath, default))
            .ReturnsAsync("content");
        await _contextService.AddFileToContextAsync(filePath);

        // Act
        await _contextService.SaveContextAsync(contextName);

        // Assert
        _fileSystemServiceMock.Verify(x => x.WriteFileAsync(
            It.Is<string>(p => p.Contains(contextName) && p.EndsWith(".json")),
            It.IsAny<string>(),
            default), Times.Once);
    }

    [Fact]
    public async Task LoadContextAsync_LoadsContextFromFile()
    {
        // Arrange
        var contextName = "test-context";
        var filePath = "test.txt";
        var contextJson = "{\"Name\":\"test-context\",\"Files\":[\"test.txt\"],\"SavedAt\":\"2024-01-01\"}";
        
        _fileSystemServiceMock.Setup(x => x.FileExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(It.Is<string>(p => p.Contains(".json")), default))
            .ReturnsAsync(contextJson);
        _fileSystemServiceMock.Setup(x => x.FileExistsAsync(filePath, default))
            .ReturnsAsync(true);
        _fileSystemServiceMock.Setup(x => x.ReadFileAsync(filePath, default))
            .ReturnsAsync("content");

        // Act
        await _contextService.LoadContextAsync(contextName);
        var files = await _contextService.GetContextFilesAsync();

        // Assert
        files.Should().Contain(filePath);
    }

    [Fact]
    public async Task LoadContextAsync_ThrowsWhenContextNotFound()
    {
        // Arrange
        var contextName = "non-existent";
        _fileSystemServiceMock.Setup(x => x.FileExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _contextService.LoadContextAsync(contextName));
    }

    [Fact]
    public async Task GetSavedContextsAsync_ReturnsListOfSavedContexts()
    {
        // Arrange
        var files = new[] { "/path/context1.json", "/path/context2.json" };
        _fileSystemServiceMock.Setup(x => x.GetFilesAsync(It.IsAny<string>(), "*.json", false, default))
            .ReturnsAsync(files);

        // Act
        var contexts = await _contextService.GetSavedContextsAsync();

        // Assert
        contexts.Should().Contain("context1");
        contexts.Should().Contain("context2");
    }
}