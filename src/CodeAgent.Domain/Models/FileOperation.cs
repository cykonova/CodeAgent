namespace CodeAgent.Domain.Models;

public class FileOperation
{
    public FileOperationType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? OriginalContent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum FileOperationType
{
    Read,
    Write,
    Create,
    Delete,
    Rename,
    Move
}