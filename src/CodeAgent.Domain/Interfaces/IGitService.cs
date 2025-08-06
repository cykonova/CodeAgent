using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IGitService
{
    Task<bool> IsRepositoryAsync(string path);
    Task<string> GetCurrentBranchAsync(string path);
    Task<string?> GetRemoteUrlAsync(string path);
    Task<bool> HasUncommittedChangesAsync(string path);
    Task<IEnumerable<string>> GetModifiedFilesAsync(string path);
    Task<IEnumerable<string>> GetUntrackedFilesAsync(string path);
    Task<bool> StageFileAsync(string path, string filePath);
    Task<bool> StageAllAsync(string path);
    Task<bool> CommitAsync(string path, string message);
    Task<bool> CreateBranchAsync(string path, string branchName);
    Task<bool> CheckoutBranchAsync(string path, string branchName);
    Task<IEnumerable<string>> GetBranchesAsync(string path);
    Task<string> GetDiffAsync(string path, string? filePath = null);
    Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string path, int count = 10);
}