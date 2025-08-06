using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeAgent.Core.Services;

public class ContextManager : IContextManager
{
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<ContextManager> _logger;
    private ProjectContext? _currentContext;
    
    private static readonly Dictionary<string, ProjectType> ProjectTypePatterns = new()
    {
        { "*.csproj", ProjectType.DotNet },
        { "*.sln", ProjectType.DotNet },
        { "package.json", ProjectType.NodeJs },
        { "requirements.txt", ProjectType.Python },
        { "setup.py", ProjectType.Python },
        { "pyproject.toml", ProjectType.Python },
        { "pom.xml", ProjectType.Java },
        { "build.gradle", ProjectType.Java },
        { "go.mod", ProjectType.Go },
        { "Cargo.toml", ProjectType.Rust },
        { "Gemfile", ProjectType.Ruby },
        { "composer.json", ProjectType.Php }
    };

    private static readonly HashSet<string> SourceExtensions = new()
    {
        ".cs", ".ts", ".js", ".tsx", ".jsx", ".py", ".java", ".go", ".rs",
        ".rb", ".php", ".cpp", ".c", ".h", ".hpp", ".swift", ".kt", ".scala"
    };

    private static readonly HashSet<string> TestPatterns = new()
    {
        "test", "tests", "spec", "specs", "__tests__", "test_", "_test"
    };

    private static readonly HashSet<string> ConfigExtensions = new()
    {
        ".json", ".yml", ".yaml", ".toml", ".ini", ".conf", ".config", ".xml"
    };

    public ContextManager(IFileSystemService fileSystem, ILogger<ContextManager> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<ProjectContext> BuildContextAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (!await _fileSystem.DirectoryExistsAsync(projectPath))
        {
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");
        }

        var context = new ProjectContext
        {
            ProjectPath = projectPath,
            ProjectName = Path.GetFileName(projectPath),
            LastUpdated = DateTime.UtcNow
        };

        // Detect project type
        context.ProjectType = await DetectProjectTypeAsync(projectPath);
        _logger.LogInformation("Detected project type: {ProjectType}", context.ProjectType);

        // Scan files
        await ScanProjectFilesAsync(context, cancellationToken);
        
        // Detect languages
        DetectLanguages(context);
        
        // Extract dependencies
        await ExtractDependenciesAsync(context);
        
        // Build file metadata
        await BuildFileMetadataAsync(context, cancellationToken);

        _currentContext = context;
        _logger.LogInformation("Built context for project: {ProjectName} with {FileCount} files",
            context.ProjectName, context.SourceFiles.Count);

        return context;
    }

    public async Task<IReadOnlyList<string>> GetRelevantFilesAsync(string query, int maxFiles = 10, CancellationToken cancellationToken = default)
    {
        if (_currentContext == null)
        {
            return Array.Empty<string>();
        }

        var scores = await CalculateRelevanceScoresAsync(query, _currentContext.SourceFiles, cancellationToken);
        
        return scores
            .OrderByDescending(kvp => kvp.Value)
            .Take(maxFiles)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public async Task UpdateContextAsync(ProjectContext context, IReadOnlyList<string> modifiedFiles, CancellationToken cancellationToken = default)
    {
        foreach (var file in modifiedFiles)
        {
            if (context.FileMetadata.ContainsKey(file))
            {
                var metadata = await BuildFileMetadataForFileAsync(file, cancellationToken);
                if (metadata != null)
                {
                    context.FileMetadata[file] = metadata;
                }
            }
        }

        context.LastUpdated = DateTime.UtcNow;
    }

    public Task<Dictionary<string, double>> CalculateRelevanceScoresAsync(string query, IReadOnlyList<string> files, CancellationToken cancellationToken = default)
    {
        var scores = new Dictionary<string, double>();
        var queryTokens = TokenizeText(query.ToLower());

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file).ToLower();
            var fileTokens = TokenizeText(fileName);
            
            // Calculate simple token overlap score
            var score = CalculateTokenOverlap(queryTokens, fileTokens);
            
            // Boost score if file path contains query terms
            var pathTokens = TokenizeText(file.ToLower());
            score += CalculateTokenOverlap(queryTokens, pathTokens) * 0.5;
            
            // Check file content if metadata is available
            if (_currentContext?.FileMetadata.TryGetValue(file, out var metadata) == true)
            {
                // Check class and function names
                var symbolTokens = new HashSet<string>();
                foreach (var className in metadata.Classes)
                {
                    symbolTokens.UnionWith(TokenizeText(className.ToLower()));
                }
                foreach (var funcName in metadata.Functions)
                {
                    symbolTokens.UnionWith(TokenizeText(funcName.ToLower()));
                }
                
                score += CalculateTokenOverlap(queryTokens, symbolTokens) * 0.7;
            }
            
            scores[file] = score;
        }

        return Task.FromResult(scores);
    }

    public ProjectContext? GetCurrentContext() => _currentContext;

    public void ClearContext()
    {
        _currentContext = null;
        _logger.LogDebug("Context cleared");
    }

    private async Task<ProjectType> DetectProjectTypeAsync(string projectPath)
    {
        foreach (var (pattern, projectType) in ProjectTypePatterns)
        {
            var matchingFiles = await _fileSystem.GetFilesAsync(projectPath, pattern, false);
            if (matchingFiles.Any())
            {
                return projectType;
            }
        }

        return ProjectType.Unknown;
    }

    private async Task ScanProjectFilesAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var allFiles = await _fileSystem.GetFilesAsync(context.ProjectPath, "*", true);
        
        foreach (var file in allFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var extension = Path.GetExtension(file);
            var fileName = Path.GetFileName(file);
            var directory = Path.GetDirectoryName(file) ?? "";
            
            // Skip common ignore patterns
            if (ShouldIgnoreFile(file))
                continue;
            
            // Categorize files
            if (IsTestFile(file, directory))
            {
                context.TestFiles.Add(file);
            }
            else if (SourceExtensions.Contains(extension))
            {
                context.SourceFiles.Add(file);
            }
            else if (ConfigExtensions.Contains(extension))
            {
                context.ConfigurationFiles.Add(file);
            }
        }
    }

    private bool ShouldIgnoreFile(string filePath)
    {
        var ignoredPatterns = new[] { "node_modules", "bin", "obj", ".git", ".vs", "dist", "build", "target" };
        return ignoredPatterns.Any(pattern => filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsTestFile(string filePath, string directory)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
        var dirName = Path.GetFileName(directory).ToLower();
        
        return TestPatterns.Any(pattern => 
            fileName.Contains(pattern) || dirName.Contains(pattern));
    }

    private void DetectLanguages(ProjectContext context)
    {
        var languageCounts = new Dictionary<string, int>();
        
        foreach (var file in context.SourceFiles)
        {
            var extension = Path.GetExtension(file);
            var language = GetLanguageFromExtension(extension);
            
            if (!string.IsNullOrEmpty(language))
            {
                languageCounts.TryGetValue(language, out var count);
                languageCounts[language] = count + 1;
            }
        }

        if (languageCounts.Any())
        {
            var sortedLanguages = languageCounts.OrderByDescending(kvp => kvp.Value).ToList();
            context.PrimaryLanguage = sortedLanguages.First().Key;
            context.SecondaryLanguages = sortedLanguages.Skip(1).Select(kvp => kvp.Key).ToList();
        }
    }

    private string GetLanguageFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".cs" => "C#",
            ".ts" or ".tsx" => "TypeScript",
            ".js" or ".jsx" => "JavaScript",
            ".py" => "Python",
            ".java" => "Java",
            ".go" => "Go",
            ".rs" => "Rust",
            ".rb" => "Ruby",
            ".php" => "PHP",
            ".cpp" or ".cc" or ".cxx" => "C++",
            ".c" => "C",
            ".swift" => "Swift",
            ".kt" or ".kts" => "Kotlin",
            ".scala" => "Scala",
            _ => ""
        };
    }

    private async Task ExtractDependenciesAsync(ProjectContext context)
    {
        switch (context.ProjectType)
        {
            case ProjectType.DotNet:
                await ExtractDotNetDependenciesAsync(context);
                break;
            case ProjectType.NodeJs:
                await ExtractNodeDependenciesAsync(context);
                break;
            case ProjectType.Python:
                await ExtractPythonDependenciesAsync(context);
                break;
            // Add more as needed
        }
    }

    private async Task ExtractDotNetDependenciesAsync(ProjectContext context)
    {
        var csprojFiles = await _fileSystem.GetFilesAsync(context.ProjectPath, "*.csproj", true);
        
        foreach (var csproj in csprojFiles)
        {
            var content = await _fileSystem.ReadFileAsync(csproj);
            var packageRefs = Regex.Matches(content, @"<PackageReference\s+Include=""([^""]+)""");
            
            foreach (Match match in packageRefs)
            {
                if (match.Groups.Count > 1)
                {
                    context.Dependencies.Add(match.Groups[1].Value);
                }
            }
        }
    }

    private async Task ExtractNodeDependenciesAsync(ProjectContext context)
    {
        var packageJsonPath = Path.Combine(context.ProjectPath, "package.json");
        if (await _fileSystem.FileExistsAsync(packageJsonPath))
        {
            var content = await _fileSystem.ReadFileAsync(packageJsonPath);
            // Simple extraction - could use JSON parsing for better accuracy
            var deps = Regex.Matches(content, @"""([^""]+)""\s*:\s*""[^""]+""");
            
            foreach (Match match in deps)
            {
                if (match.Groups.Count > 1)
                {
                    context.Dependencies.Add(match.Groups[1].Value);
                }
            }
        }
    }

    private async Task ExtractPythonDependenciesAsync(ProjectContext context)
    {
        var requirementsPath = Path.Combine(context.ProjectPath, "requirements.txt");
        if (await _fileSystem.FileExistsAsync(requirementsPath))
        {
            var content = await _fileSystem.ReadFileAsync(requirementsPath);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var dep = line.Split(new[] { '=', '>', '<', '~' })[0].Trim();
                if (!string.IsNullOrEmpty(dep) && !dep.StartsWith("#"))
                {
                    context.Dependencies.Add(dep);
                }
            }
        }
    }

    private async Task BuildFileMetadataAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        foreach (var file in context.SourceFiles.Take(100)) // Limit for performance
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var metadata = await BuildFileMetadataForFileAsync(file, cancellationToken);
            if (metadata != null)
            {
                context.FileMetadata[file] = metadata;
            }
        }
    }

    private async Task<FileMetadata?> BuildFileMetadataForFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var content = await _fileSystem.ReadFileAsync(filePath);
            
            var metadata = new FileMetadata
            {
                FilePath = filePath,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTimeUtc,
                FileHash = ComputeFileHash(content),
                LineCount = content.Count(c => c == '\n') + 1
            };

            // Extract code elements based on file extension
            var extension = Path.GetExtension(filePath);
            ExtractCodeElements(content, extension, metadata);
            
            // Calculate complexity (simple line-based metric)
            metadata.ComplexityScore = CalculateComplexity(content);
            
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build metadata for file: {FilePath}", filePath);
            return null;
        }
    }

    private void ExtractCodeElements(string content, string extension, FileMetadata metadata)
    {
        switch (extension.ToLower())
        {
            case ".cs":
                ExtractCSharpElements(content, metadata);
                break;
            case ".ts":
            case ".js":
                ExtractJavaScriptElements(content, metadata);
                break;
            case ".py":
                ExtractPythonElements(content, metadata);
                break;
            // Add more languages as needed
        }
    }

    private void ExtractCSharpElements(string content, FileMetadata metadata)
    {
        // Extract using statements
        var usingMatches = Regex.Matches(content, @"using\s+([\w\.]+);");
        foreach (Match match in usingMatches)
        {
            if (match.Groups.Count > 1)
                metadata.Imports.Add(match.Groups[1].Value);
        }

        // Extract class names
        var classMatches = Regex.Matches(content, @"(?:public|private|internal|protected)?\s*(?:partial|static|abstract|sealed)?\s*class\s+(\w+)");
        foreach (Match match in classMatches)
        {
            if (match.Groups.Count > 1)
                metadata.Classes.Add(match.Groups[1].Value);
        }

        // Extract method names
        var methodMatches = Regex.Matches(content, @"(?:public|private|internal|protected)\s+(?:static\s+)?(?:async\s+)?(?:Task<?\w*>?|void|\w+)\s+(\w+)\s*\(");
        foreach (Match match in methodMatches)
        {
            if (match.Groups.Count > 1)
                metadata.Functions.Add(match.Groups[1].Value);
        }
    }

    private void ExtractJavaScriptElements(string content, FileMetadata metadata)
    {
        // Extract imports
        var importMatches = Regex.Matches(content, @"import\s+.*?\s+from\s+['""]([^'""]+)['""]");
        foreach (Match match in importMatches)
        {
            if (match.Groups.Count > 1)
                metadata.Imports.Add(match.Groups[1].Value);
        }

        // Extract class names
        var classMatches = Regex.Matches(content, @"class\s+(\w+)");
        foreach (Match match in classMatches)
        {
            if (match.Groups.Count > 1)
                metadata.Classes.Add(match.Groups[1].Value);
        }

        // Extract function names
        var funcMatches = Regex.Matches(content, @"(?:function\s+(\w+)|const\s+(\w+)\s*=\s*(?:async\s+)?(?:\([^)]*\)|[^=]*)?\s*=>)");
        foreach (Match match in funcMatches)
        {
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (!string.IsNullOrEmpty(match.Groups[i].Value))
                    metadata.Functions.Add(match.Groups[i].Value);
            }
        }
    }

    private void ExtractPythonElements(string content, FileMetadata metadata)
    {
        // Extract imports
        var importMatches = Regex.Matches(content, @"(?:from\s+([\w\.]+)\s+)?import\s+([\w\.,\s]+)");
        foreach (Match match in importMatches)
        {
            if (match.Groups.Count > 1 && !string.IsNullOrEmpty(match.Groups[1].Value))
                metadata.Imports.Add(match.Groups[1].Value);
            if (match.Groups.Count > 2)
                metadata.Imports.Add(match.Groups[2].Value);
        }

        // Extract class names
        var classMatches = Regex.Matches(content, @"class\s+(\w+)");
        foreach (Match match in classMatches)
        {
            if (match.Groups.Count > 1)
                metadata.Classes.Add(match.Groups[1].Value);
        }

        // Extract function names
        var funcMatches = Regex.Matches(content, @"def\s+(\w+)\s*\(");
        foreach (Match match in funcMatches)
        {
            if (match.Groups.Count > 1)
                metadata.Functions.Add(match.Groups[1].Value);
        }
    }

    private double CalculateComplexity(string content)
    {
        // Simple cyclomatic complexity approximation
        var complexity = 1.0;
        
        // Count decision points
        complexity += Regex.Matches(content, @"\b(if|else|elif|switch|case|for|while|foreach|catch)\b").Count;
        
        // Count logical operators
        complexity += Regex.Matches(content, @"(\|\||&&|\band\b|\bor\b)").Count * 0.5;
        
        // Normalize by line count
        var lineCount = content.Count(c => c == '\n') + 1;
        return Math.Round(complexity / Math.Max(lineCount / 100.0, 1), 2);
    }

    private string ComputeFileHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Substring(0, 8);
    }

    private HashSet<string> TokenizeText(string text)
    {
        var tokens = new HashSet<string>();
        var words = Regex.Split(text, @"[^a-zA-Z0-9]+");
        
        foreach (var word in words)
        {
            if (!string.IsNullOrEmpty(word))
            {
                tokens.Add(word);
                
                // Also add camelCase/PascalCase splits
                var subWords = Regex.Split(word, @"(?=[A-Z][a-z])|(?<=[a-z])(?=[A-Z])");
                foreach (var subWord in subWords)
                {
                    if (subWord.Length > 1)
                        tokens.Add(subWord.ToLower());
                }
            }
        }
        
        return tokens;
    }

    private double CalculateTokenOverlap(HashSet<string> tokens1, HashSet<string> tokens2)
    {
        if (!tokens1.Any() || !tokens2.Any())
            return 0;
        
        var intersection = tokens1.Intersect(tokens2).Count();
        var union = tokens1.Union(tokens2).Count();
        
        return (double)intersection / union;
    }
}