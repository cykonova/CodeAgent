using System.Diagnostics;
using System.Text;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;

namespace CodeAgent.Infrastructure.Services;

public class GitService : IGitService
{
    public async Task<bool> IsRepositoryAsync(string path)
    {
        return await Task.Run(() => Directory.Exists(Path.Combine(path, ".git")));
    }

    public async Task<string> GetCurrentBranchAsync(string path)
    {
        var result = await RunGitCommandAsync(path, "branch --show-current");
        return result.Trim();
    }

    public async Task<string?> GetRemoteUrlAsync(string path)
    {
        try
        {
            var result = await RunGitCommandAsync(path, "remote get-url origin");
            return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> HasUncommittedChangesAsync(string path)
    {
        var result = await RunGitCommandAsync(path, "status --porcelain");
        return !string.IsNullOrWhiteSpace(result);
    }

    public async Task<IEnumerable<string>> GetModifiedFilesAsync(string path)
    {
        var result = await RunGitCommandAsync(path, "diff --name-only");
        return result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public async Task<IEnumerable<string>> GetUntrackedFilesAsync(string path)
    {
        var result = await RunGitCommandAsync(path, "ls-files --others --exclude-standard");
        return result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public async Task<bool> StageFileAsync(string path, string filePath)
    {
        try
        {
            await RunGitCommandAsync(path, $"add \"{filePath}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StageAllAsync(string path)
    {
        try
        {
            await RunGitCommandAsync(path, "add .");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CommitAsync(string path, string message)
    {
        try
        {
            // Escape quotes in message
            var escapedMessage = message.Replace("\"", "\\\"");
            await RunGitCommandAsync(path, $"commit -m \"{escapedMessage}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateBranchAsync(string path, string branchName)
    {
        try
        {
            await RunGitCommandAsync(path, $"checkout -b \"{branchName}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CheckoutBranchAsync(string path, string branchName)
    {
        try
        {
            await RunGitCommandAsync(path, $"checkout \"{branchName}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetBranchesAsync(string path)
    {
        var result = await RunGitCommandAsync(path, "branch -a");
        return result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim().TrimStart('*').Trim());
    }

    public async Task<string> GetDiffAsync(string path, string? filePath = null)
    {
        var command = filePath != null ? $"diff \"{filePath}\"" : "diff";
        return await RunGitCommandAsync(path, command);
    }

    public async Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string path, int count = 10)
    {
        var commits = new List<GitCommit>();
        var result = await RunGitCommandAsync(path, $"log -{count} --pretty=format:\"%H|%an|%ae|%ad|%s\" --date=iso");
        
        foreach (var line in result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            if (parts.Length >= 5)
            {
                commits.Add(new GitCommit
                {
                    Id = parts[0],
                    Author = parts[1],
                    Email = parts[2],
                    Date = DateTime.TryParse(parts[3], out var date) ? date : DateTime.MinValue,
                    Message = parts[4]
                });
            }
        }
        
        return commits;
    }

    private async Task<string> RunGitCommandAsync(string workingDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}