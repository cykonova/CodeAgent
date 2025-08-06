using CodeAgent.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CodeAgent.Infrastructure.Tests;

public class DiffServiceTests
{
    private readonly DiffService _diffService;

    public DiffServiceTests()
    {
        _diffService = new DiffService();
    }

    [Fact]
    public async Task GenerateDiffAsync_WithNoChanges_ReturnsEmptyDiff()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3";

        // Act
        var result = await _diffService.GenerateDiffAsync(content, content, "test.txt");

        // Assert
        result.Should().NotBeNull();
        result.HasChanges.Should().BeFalse();
        result.AddedLines.Should().Be(0);
        result.DeletedLines.Should().Be(0);
        result.ModifiedLines.Should().Be(0);
    }

    [Fact]
    public async Task GenerateDiffAsync_WithAddedLines_CountsCorrectly()
    {
        // Arrange
        var original = "Line 1\nLine 2";
        var modified = "Line 1\nLine 2\nLine 3\nLine 4";

        // Act
        var result = await _diffService.GenerateDiffAsync(original, modified, "test.txt");

        // Assert
        result.HasChanges.Should().BeTrue();
        result.AddedLines.Should().Be(2);
        result.DeletedLines.Should().Be(0);
    }

    [Fact]
    public async Task GenerateDiffAsync_WithDeletedLines_CountsCorrectly()
    {
        // Arrange
        var original = "Line 1\nLine 2\nLine 3";
        var modified = "Line 1";

        // Act
        var result = await _diffService.GenerateDiffAsync(original, modified, "test.txt");

        // Assert
        result.HasChanges.Should().BeTrue();
        result.DeletedLines.Should().Be(2);
        result.AddedLines.Should().Be(0);
    }

    [Fact]
    public async Task GenerateDiffAsync_WithModifiedLines_CountsAsDeleteAndAdd()
    {
        // Arrange
        var original = "Line 1\nLine 2\nLine 3";
        var modified = "Line 1\nModified Line 2\nLine 3";

        // Act
        var result = await _diffService.GenerateDiffAsync(original, modified, "test.txt");

        // Assert
        result.HasChanges.Should().BeTrue();
        result.AddedLines.Should().Be(1);
        result.DeletedLines.Should().Be(1);
    }

    [Fact]
    public async Task CompareLinesAsync_ReturnsCorrectLineTypes()
    {
        // Arrange
        var original = "Line 1\nLine 2";
        var modified = "Line 1\nModified Line\nLine 3";

        // Act
        var lines = await _diffService.CompareLinesAsync(original, modified);
        var lineList = lines.ToList();

        // Assert
        lineList.Should().HaveCountGreaterThan(0);
        lineList.Should().Contain(l => l.Type == Domain.Models.DiffLineType.Unchanged);
        lineList.Should().Contain(l => l.Type == Domain.Models.DiffLineType.Added);
        lineList.Should().Contain(l => l.Type == Domain.Models.DiffLineType.Deleted);
    }

    [Fact]
    public async Task GenerateUnifiedDiffAsync_ProducesValidDiffOutput()
    {
        // Arrange
        var original = "Line 1\nLine 2\nLine 3";
        var modified = "Line 1\nModified Line 2\nLine 3\nLine 4";

        // Act
        var diff = await _diffService.GenerateUnifiedDiffAsync(original, modified, "test.txt");

        // Assert
        diff.Should().NotBeNullOrEmpty();
        diff.Should().Contain("--- test.txt");
        diff.Should().Contain("+++ test.txt");
        diff.Should().Contain("@@");
    }

    [Fact]
    public async Task GenerateDiffAsync_WithEmptyOriginal_HandlesCorrectly()
    {
        // Arrange
        var original = "";
        var modified = "New content";

        // Act
        var result = await _diffService.GenerateDiffAsync(original, modified, "new.txt");

        // Assert
        result.HasChanges.Should().BeTrue();
        result.AddedLines.Should().BeGreaterThan(0);
        // When original is empty, it might show as deleted empty line + added content
    }

    [Fact]
    public async Task GenerateDiffAsync_WithEmptyModified_HandlesCorrectly()
    {
        // Arrange
        var original = "Old content";
        var modified = "";

        // Act
        var result = await _diffService.GenerateDiffAsync(original, modified, "deleted.txt");

        // Assert
        result.HasChanges.Should().BeTrue();
        result.DeletedLines.Should().BeGreaterThan(0);
        // When modified is empty, it might show as deleted content + added empty line
    }
}