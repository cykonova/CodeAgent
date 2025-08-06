using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;

namespace CodeAgent.Infrastructure.Services;

public class FileSystemService : IFileSystemService
{
    private readonly List<FileOperation> _operations = new();

    public async Task<string> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        await TrackOperationAsync(new FileOperation
        {
            Type = FileOperationType.Read,
            FilePath = path,
            Content = content
        }, cancellationToken);
        return content;
    }

    public async Task WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        string? originalContent = null;
        if (File.Exists(path))
        {
            originalContent = await File.ReadAllTextAsync(path, cancellationToken);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken);
        
        await TrackOperationAsync(new FileOperation
        {
            Type = FileOperationType.Write,
            FilePath = path,
            Content = content,
            OriginalContent = originalContent
        }, cancellationToken);
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(path));
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        string? originalContent = null;
        if (File.Exists(path))
        {
            originalContent = await File.ReadAllTextAsync(path, cancellationToken);
            File.Delete(path);
        }

        await TrackOperationAsync(new FileOperation
        {
            Type = FileOperationType.Delete,
            FilePath = path,
            OriginalContent = originalContent
        }, cancellationToken);
    }

    public Task<string[]> GetFilesAsync(string path, string pattern = "*", bool recursive = false, CancellationToken cancellationToken = default)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(path, pattern, searchOption);
        return Task.FromResult(files);
    }

    public Task<string[]> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default)
    {
        var directories = Directory.GetDirectories(path);
        return Task.FromResult(directories);
    }

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task<FileOperation> TrackOperationAsync(FileOperation operation, CancellationToken cancellationToken = default)
    {
        _operations.Add(operation);
        return Task.FromResult(operation);
    }

    public List<FileOperation> GetOperationHistory()
    {
        return new List<FileOperation>(_operations);
    }

    public void ClearOperationHistory()
    {
        _operations.Clear();
    }
}