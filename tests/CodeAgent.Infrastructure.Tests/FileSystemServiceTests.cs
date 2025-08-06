using CodeAgent.Infrastructure.Services;
using FluentAssertions;

namespace CodeAgent.Infrastructure.Tests;

public class FileSystemServiceTests : IDisposable
{
    private readonly FileSystemService _service;
    private readonly string _testDirectory;

    public FileSystemServiceTests()
    {
        _service = new FileSystemService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CodeAgentTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task ReadFileAsync_ShouldReadFileContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var expectedContent = "Hello, World!";
        await File.WriteAllTextAsync(filePath, expectedContent);

        // Act
        var content = await _service.ReadFileAsync(filePath);

        // Assert
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task WriteFileAsync_ShouldCreateFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "output.txt");
        var content = "Test content";

        // Act
        await _service.WriteFileAsync(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(filePath);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async Task FileExistsAsync_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "existing.txt");
        await File.WriteAllTextAsync(filePath, "content");

        // Act
        var exists = await _service.FileExistsAsync(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var exists = await _service.FileExistsAsync(filePath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "delete.txt");
        await File.WriteAllTextAsync(filePath, "to be deleted");

        // Act
        await _service.DeleteFileAsync(filePath);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task GetFilesAsync_ShouldReturnMatchingFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file1.txt"), "");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file2.txt"), "");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file3.log"), "");

        // Act
        var files = await _service.GetFilesAsync(_testDirectory, "*.txt");

        // Assert
        files.Should().HaveCount(2);
        files.Should().Contain(f => f.EndsWith("file1.txt"));
        files.Should().Contain(f => f.EndsWith("file2.txt"));
        files.Should().NotContain(f => f.EndsWith("file3.log"));
    }

    [Fact]
    public async Task GetFilesAsync_WithRecursive_ShouldSearchSubdirectories()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "root.txt"), "");
        await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "");

        // Act
        var files = await _service.GetFilesAsync(_testDirectory, "*.txt", recursive: true);

        // Assert
        files.Should().HaveCount(2);
        files.Should().Contain(f => f.EndsWith("root.txt"));
        files.Should().Contain(f => f.EndsWith("sub.txt"));
    }

    [Fact]
    public async Task CreateDirectoryAsync_ShouldCreateDirectory()
    {
        // Arrange
        var dirPath = Path.Combine(_testDirectory, "newdir");

        // Act
        await _service.CreateDirectoryAsync(dirPath);

        // Assert
        Directory.Exists(dirPath).Should().BeTrue();
    }

    [Fact]
    public async Task GetDirectoriesAsync_ShouldReturnSubdirectories()
    {
        // Arrange
        var subDir1 = Path.Combine(_testDirectory, "sub1");
        var subDir2 = Path.Combine(_testDirectory, "sub2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        // Act
        var directories = await _service.GetDirectoriesAsync(_testDirectory);

        // Assert
        directories.Should().HaveCount(2);
        directories.Should().Contain(d => d.EndsWith("sub1"));
        directories.Should().Contain(d => d.EndsWith("sub2"));
    }

    [Fact]
    public async Task GetProjectFilesAsync_ShouldExcludeBuildDirectories()
    {
        // Arrange
        var binDir = Path.Combine(_testDirectory, "bin");
        var objDir = Path.Combine(_testDirectory, "obj");
        var srcDir = Path.Combine(_testDirectory, "src");
        
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);
        Directory.CreateDirectory(srcDir);
        
        await File.WriteAllTextAsync(Path.Combine(binDir, "binary.dll"), "");
        await File.WriteAllTextAsync(Path.Combine(objDir, "object.o"), "");
        await File.WriteAllTextAsync(Path.Combine(srcDir, "source.cs"), "");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "readme.md"), "");

        // Act
        var files = await _service.GetProjectFilesAsync(_testDirectory);

        // Assert
        files.Should().HaveCount(2);
        files.Should().Contain(f => f.EndsWith("source.cs"));
        files.Should().Contain(f => f.EndsWith("readme.md"));
        files.Should().NotContain(f => f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
        files.Should().NotContain(f => f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}