using CodeAgent.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace CodeAgent.Core.Services;

public class ContextService : IContextService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly List<string> _contextFiles = new();
    private readonly Dictionary<string, string> _fileContents = new();
    private readonly string _contextDirectory;

    public ContextService(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _contextDirectory = Path.Combine(homeDir, ".codeagent", "contexts");
        Directory.CreateDirectory(_contextDirectory);
    }

    public async Task AddFileToContextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!_contextFiles.Contains(filePath))
        {
            _contextFiles.Add(filePath);
            var content = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);
            _fileContents[filePath] = content;
        }
    }

    public async Task RemoveFileFromContextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _contextFiles.Remove(filePath);
            _fileContents.Remove(filePath);
        }, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetContextFilesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_contextFiles.ToList());
    }

    public async Task ClearContextAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _contextFiles.Clear();
            _fileContents.Clear();
        }, cancellationToken);
    }

    public async Task<string> GetContextSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = new StringBuilder();
        summary.AppendLine($"Context contains {_contextFiles.Count} files:");
        summary.AppendLine();
        
        foreach (var file in _contextFiles)
        {
            if (_fileContents.TryGetValue(file, out var content))
            {
                var lines = content.Split('\n').Length;
                var size = content.Length;
                summary.AppendLine($"  • {file} ({lines} lines, {size:N0} chars)");
            }
        }
        
        var totalSize = _fileContents.Values.Sum(c => c.Length);
        var totalLines = _fileContents.Values.Sum(c => c.Split('\n').Length);
        
        summary.AppendLine();
        summary.AppendLine($"Total: {totalLines:N0} lines, {totalSize:N0} characters");
        
        return await Task.FromResult(summary.ToString());
    }

    public async Task<string> BuildPromptContextAsync(string basePrompt, CancellationToken cancellationToken = default)
    {
        var promptBuilder = new StringBuilder();
        
        // Add base prompt
        promptBuilder.AppendLine(basePrompt);
        promptBuilder.AppendLine();
        
        // Add context files if any
        if (_contextFiles.Any())
        {
            promptBuilder.AppendLine("=== CONTEXT FILES ===");
            promptBuilder.AppendLine();
            
            foreach (var file in _contextFiles)
            {
                if (_fileContents.TryGetValue(file, out var content))
                {
                    promptBuilder.AppendLine($"File: {file}");
                    promptBuilder.AppendLine("---");
                    
                    // Limit content size to prevent token overflow
                    if (content.Length > 3000)
                    {
                        promptBuilder.AppendLine(content.Substring(0, 3000));
                        promptBuilder.AppendLine($"... (truncated, {content.Length - 3000} chars remaining)");
                    }
                    else
                    {
                        promptBuilder.AppendLine(content);
                    }
                    
                    promptBuilder.AppendLine();
                }
            }
            
            promptBuilder.AppendLine("=== END CONTEXT ===");
            promptBuilder.AppendLine();
        }
        
        return await Task.FromResult(promptBuilder.ToString());
    }

    public async Task SaveContextAsync(string name, CancellationToken cancellationToken = default)
    {
        var contextData = new ContextData
        {
            Name = name,
            Files = _contextFiles.ToList(),
            SavedAt = DateTime.UtcNow
        };
        
        var json = JsonSerializer.Serialize(contextData, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(_contextDirectory, $"{name}.json");
        
        await _fileSystemService.WriteFileAsync(filePath, json, cancellationToken);
    }

    public async Task LoadContextAsync(string name, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_contextDirectory, $"{name}.json");
        
        if (!await _fileSystemService.FileExistsAsync(filePath, cancellationToken))
        {
            throw new FileNotFoundException($"Context '{name}' not found");
        }
        
        var json = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);
        var contextData = JsonSerializer.Deserialize<ContextData>(json);
        
        if (contextData != null)
        {
            _contextFiles.Clear();
            _fileContents.Clear();
            
            foreach (var file in contextData.Files)
            {
                if (await _fileSystemService.FileExistsAsync(file, cancellationToken))
                {
                    await AddFileToContextAsync(file, cancellationToken);
                }
            }
        }
    }

    public async Task<IEnumerable<string>> GetSavedContextsAsync(CancellationToken cancellationToken = default)
    {
        var files = await _fileSystemService.GetFilesAsync(_contextDirectory, "*.json", false, cancellationToken);
        return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
    }

    private class ContextData
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new();
        public DateTime SavedAt { get; set; }
    }
}