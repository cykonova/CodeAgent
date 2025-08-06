namespace CodeAgent.Domain.Models;

public class DiffLine
{
    public DiffLineType Type { get; set; }
    public int? OriginalLineNumber { get; set; }
    public int? ModifiedLineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
}