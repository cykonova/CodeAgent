namespace CodeAgent.Domain.Models;

public class DiffResult
{
    public string? FileName { get; set; }
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    public int ModifiedLines { get; set; }
    public bool HasChanges => AddedLines > 0 || DeletedLines > 0 || ModifiedLines > 0;
    public List<DiffLine> Lines { get; set; } = new();
    public string UnifiedDiff { get; set; } = string.Empty;
}