using CodeAgent.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CodeAgent.Infrastructure.Tests;

public class GitServiceTests : IDisposable
{
    private readonly GitService _gitService;
    private readonly string _testRepoPath;

    public GitServiceTests()
    {
        _gitService = new GitService();
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"git-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
    }

    [Fact]
    public async Task IsRepositoryAsync_WithGitRepo_ReturnsTrue()
    {
        // Arrange
        var gitDir = Path.Combine(_testRepoPath, ".git");
        Directory.CreateDirectory(gitDir);

        // Act
        var isRepo = await _gitService.IsRepositoryAsync(_testRepoPath);

        // Assert
        isRepo.Should().BeTrue();
    }

    [Fact]
    public async Task IsRepositoryAsync_WithoutGitRepo_ReturnsFalse()
    {
        // Act
        var isRepo = await _gitService.IsRepositoryAsync(_testRepoPath);

        // Assert
        isRepo.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentBranchAsync_ReturnsCurrentBranch()
    {
        // This test would require an actual Git repository
        // For unit testing, we might want to mock the process execution
        // or create an actual test repository
        
        // Arrange
        InitializeTestRepo();

        // Act
        var branch = await _gitService.GetCurrentBranchAsync(_testRepoPath);

        // Assert
        branch.Should().NotBeNullOrEmpty();
        // Default branch name varies (main/master)
        branch.Should().BeOneOf("main", "master");
    }

    [Fact]
    public async Task GetRemoteUrlAsync_WithNoRemote_ReturnsNull()
    {
        // Arrange
        InitializeTestRepo();

        // Act
        var remoteUrl = await _gitService.GetRemoteUrlAsync(_testRepoPath);

        // Assert
        remoteUrl.Should().BeNull();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithCleanRepo_ReturnsFalse()
    {
        // Arrange
        InitializeTestRepo();

        // Act
        var hasChanges = await _gitService.HasUncommittedChangesAsync(_testRepoPath);

        // Assert
        hasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithModifiedFiles_ReturnsTrue()
    {
        // Arrange
        InitializeTestRepo();
        var testFile = Path.Combine(_testRepoPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        // Act
        var hasChanges = await _gitService.HasUncommittedChangesAsync(_testRepoPath);

        // Assert
        hasChanges.Should().BeTrue();
    }

    [Fact]
    public async Task GetModifiedFilesAsync_ReturnsModifiedFiles()
    {
        // Arrange
        InitializeTestRepo();
        var testFile = Path.Combine(_testRepoPath, "existing.txt");
        await File.WriteAllTextAsync(testFile, "initial content");
        await RunGitCommand("add .");
        await RunGitCommand("commit -m \"Initial commit\"");
        
        await File.WriteAllTextAsync(testFile, "modified content");

        // Act
        var modifiedFiles = await _gitService.GetModifiedFilesAsync(_testRepoPath);

        // Assert
        modifiedFiles.Should().Contain("existing.txt");
    }

    [Fact]
    public async Task GetUntrackedFilesAsync_ReturnsUntrackedFiles()
    {
        // Arrange
        InitializeTestRepo();
        var untrackedFile = Path.Combine(_testRepoPath, "untracked.txt");
        await File.WriteAllTextAsync(untrackedFile, "untracked content");

        // Act
        var untrackedFiles = await _gitService.GetUntrackedFilesAsync(_testRepoPath);

        // Assert
        untrackedFiles.Should().Contain("untracked.txt");
    }

    [Fact]
    public async Task StageFileAsync_StagesFile()
    {
        // Arrange
        InitializeTestRepo();
        var testFile = Path.Combine(_testRepoPath, "stage.txt");
        await File.WriteAllTextAsync(testFile, "stage content");

        // Act
        var success = await _gitService.StageFileAsync(_testRepoPath, "stage.txt");
        var status = await RunGitCommand("status --porcelain");

        // Assert
        success.Should().BeTrue();
        status.Should().Contain("A  stage.txt");
    }

    [Fact]
    public async Task StageAllAsync_StagesAllFiles()
    {
        // Arrange
        InitializeTestRepo();
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "file1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "file2.txt"), "content2");

        // Act
        var success = await _gitService.StageAllAsync(_testRepoPath);
        var status = await RunGitCommand("status --porcelain");

        // Assert
        success.Should().BeTrue();
        status.Should().Contain("A  file1.txt");
        status.Should().Contain("A  file2.txt");
    }

    [Fact]
    public async Task CommitAsync_CreatesCommit()
    {
        // Arrange
        InitializeTestRepo();
        var testFile = Path.Combine(_testRepoPath, "commit.txt");
        await File.WriteAllTextAsync(testFile, "commit content");
        await _gitService.StageAllAsync(_testRepoPath);

        // Act
        var success = await _gitService.CommitAsync(_testRepoPath, "Test commit message");
        var log = await RunGitCommand("log --oneline -1");

        // Assert
        success.Should().BeTrue();
        log.Should().Contain("Test commit message");
    }

    [Fact]
    public async Task CreateBranchAsync_CreatesAndCheckoutsBranch()
    {
        // Arrange
        InitializeTestRepo();
        await CreateInitialCommit();

        // Act
        var success = await _gitService.CreateBranchAsync(_testRepoPath, "feature/test-branch");
        var currentBranch = await _gitService.GetCurrentBranchAsync(_testRepoPath);

        // Assert
        success.Should().BeTrue();
        currentBranch.Should().Be("feature/test-branch");
    }

    [Fact]
    public async Task CheckoutBranchAsync_SwitchesToBranch()
    {
        // Arrange
        InitializeTestRepo();
        await CreateInitialCommit();
        await _gitService.CreateBranchAsync(_testRepoPath, "feature/branch1");
        await _gitService.CreateBranchAsync(_testRepoPath, "feature/branch2");

        // Act
        var success = await _gitService.CheckoutBranchAsync(_testRepoPath, "feature/branch1");
        var currentBranch = await _gitService.GetCurrentBranchAsync(_testRepoPath);

        // Assert
        success.Should().BeTrue();
        currentBranch.Should().Be("feature/branch1");
    }

    [Fact]
    public async Task GetBranchesAsync_ReturnsAllBranches()
    {
        // Arrange
        InitializeTestRepo();
        await CreateInitialCommit();
        await _gitService.CreateBranchAsync(_testRepoPath, "feature/branch1");
        await _gitService.CreateBranchAsync(_testRepoPath, "feature/branch2");

        // Act
        var branches = await _gitService.GetBranchesAsync(_testRepoPath);

        // Assert
        branches.Should().Contain("feature/branch1");
        branches.Should().Contain("feature/branch2");
        branches.Should().Contain(b => b == "main" || b == "master");
    }

    [Fact]
    public async Task GetDiffAsync_ReturnsDiff()
    {
        // Arrange
        InitializeTestRepo();
        var testFile = Path.Combine(_testRepoPath, "diff.txt");
        await File.WriteAllTextAsync(testFile, "initial");
        await _gitService.StageAllAsync(_testRepoPath);
        await _gitService.CommitAsync(_testRepoPath, "Initial");
        
        await File.WriteAllTextAsync(testFile, "modified");

        // Act
        var diff = await _gitService.GetDiffAsync(_testRepoPath);

        // Assert
        diff.Should().NotBeNullOrEmpty();
        diff.Should().Contain("diff.txt");
        diff.Should().Contain("-initial");
        diff.Should().Contain("+modified");
    }

    [Fact]
    public async Task GetCommitHistoryAsync_ReturnsCommits()
    {
        // Arrange
        InitializeTestRepo();
        await CreateInitialCommit();
        
        var file = Path.Combine(_testRepoPath, "history.txt");
        for (int i = 1; i <= 3; i++)
        {
            await File.WriteAllTextAsync(file, $"content {i}");
            await _gitService.StageAllAsync(_testRepoPath);
            await _gitService.CommitAsync(_testRepoPath, $"Commit {i}");
        }

        // Act
        var history = await _gitService.GetCommitHistoryAsync(_testRepoPath, 5);

        // Assert
        history.Should().HaveCountGreaterOrEqualTo(3);
        history.Should().Contain(c => c.Message == "Commit 3");
        history.Should().Contain(c => c.Message == "Commit 2");
        history.Should().Contain(c => c.Message == "Commit 1");
    }

    private void InitializeTestRepo()
    {
        // Initialize a Git repository in the test path
        RunGitCommand("init").Wait();
        RunGitCommand("config user.email \"test@example.com\"").Wait();
        RunGitCommand("config user.name \"Test User\"").Wait();
    }

    private async Task CreateInitialCommit()
    {
        var file = Path.Combine(_testRepoPath, "initial.txt");
        await File.WriteAllTextAsync(file, "initial");
        await _gitService.StageAllAsync(_testRepoPath);
        await _gitService.CommitAsync(_testRepoPath, "Initial commit");
    }

    private async Task<string> RunGitCommand(string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _testRepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return output;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRepoPath))
        {
            // Remove read-only attributes from .git directory
            var gitDir = Path.Combine(_testRepoPath, ".git");
            if (Directory.Exists(gitDir))
            {
                foreach (var file in Directory.GetFiles(gitDir, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
            }
            
            Directory.Delete(_testRepoPath, true);
        }
    }
}