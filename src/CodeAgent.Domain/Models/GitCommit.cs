namespace CodeAgent.Domain.Models;

public class GitCommit
{
    public string Id { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ShortId => Id.Length >= 7 ? Id.Substring(0, 7) : Id;
    public List<string> ModifiedFiles { get; set; } = new();
}