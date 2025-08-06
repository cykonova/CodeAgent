using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IFileSystemService
{
    Task<string> ReadFileAsync(string path, CancellationToken cancellationToken = default);
    Task WriteFileAsync(string path, string content, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    Task<string[]> GetFilesAsync(string path, string pattern = "*", bool recursive = false, CancellationToken cancellationToken = default);
    Task<string[]> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default);
    Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);
    Task<FileOperation> TrackOperationAsync(FileOperation operation, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetProjectFilesAsync(string path, CancellationToken cancellationToken = default);
}