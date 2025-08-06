namespace CodeAgent.Domain.Models;

public class ProjectContext
{
    public string ProjectPath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public ProjectType ProjectType { get; set; }
    public List<string> SourceFiles { get; set; } = new();
    public List<string> TestFiles { get; set; } = new();
    public List<string> ConfigurationFiles { get; set; } = new();
    public Dictionary<string, FileMetadata> FileMetadata { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public string? PrimaryLanguage { get; set; }
    public List<string> SecondaryLanguages { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, object> CustomData { get; set; } = new();
}

public enum ProjectType
{
    Unknown,
    DotNet,
    NodeJs,
    Python,
    Java,
    Go,
    Rust,
    Ruby,
    Php,
    Mixed
}

public class FileMetadata
{
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public List<string> Imports { get; set; } = new();
    public List<string> Exports { get; set; } = new();
    public List<string> Classes { get; set; } = new();
    public List<string> Functions { get; set; } = new();
    public int LineCount { get; set; }
    public double ComplexityScore { get; set; }
}