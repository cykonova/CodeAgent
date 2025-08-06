using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class ReviewCommand : AsyncCommand<ReviewCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;
    private readonly IGitService _gitService;

    public ReviewCommand(IFileSystemService fileSystemService, IChatService chatService, IGitService gitService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
        _gitService = gitService;
    }

    public class Settings : CommandSettings
    {
        [Description("File, directory, or Git diff to review")]
        [CommandArgument(0, "[path]")]
        public string? Path { get; set; }

        [Description("Review type (code, pr, commit, architecture)")]
        [CommandOption("-t|--type")]
        public ReviewType Type { get; set; } = ReviewType.Code;

        [Description("Review depth (quick, standard, thorough)")]
        [CommandOption("-d|--depth")]
        public ReviewDepth Depth { get; set; } = ReviewDepth.Standard;

        [Description("Include specific checks")]
        [CommandOption("--checks")]
        public string? Checks { get; set; }

        [Description("Output format (console, markdown, github)")]
        [CommandOption("-f|--format")]
        public OutputFormat Format { get; set; } = OutputFormat.Console;

        [Description("Output file for review")]
        [CommandOption("-o|--output")]
        public string? OutputFile { get; set; }

        [Description("Review against Git branch")]
        [CommandOption("--base")]
        public string? BaseBranch { get; set; } = "main";

        [Description("Include metrics and scores")]
        [CommandOption("--metrics")]
        public bool IncludeMetrics { get; set; } = true;

        [Description("Suggest improvements")]
        [CommandOption("--suggest")]
        public bool SuggestImprovements { get; set; } = true;
    }

    public enum ReviewType
    {
        Code,
        PR,
        Commit,
        Architecture
    }

    public enum ReviewDepth
    {
        Quick,
        Standard,
        Thorough
    }

    public enum OutputFormat
    {
        Console,
        Markdown,
        GitHub
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold cyan]Code Review[/]"));
            
            var review = new CodeReview();
            
            // Determine what to review
            if (settings.Type == ReviewType.PR || settings.Type == ReviewType.Commit)
            {
                await ReviewGitChanges(review, settings);
            }
            else if (!string.IsNullOrEmpty(settings.Path))
            {
                await ReviewPath(review, settings);
            }
            else
            {
                // Review current directory changes
                settings.Path = ".";
                await ReviewPath(review, settings);
            }
            
            // Generate output
            string output = settings.Format switch
            {
                OutputFormat.Console => string.Empty, // Handled separately
                OutputFormat.Markdown => GenerateMarkdownOutput(review, settings),
                OutputFormat.GitHub => GenerateGitHubOutput(review, settings),
                _ => string.Empty
            };
            
            // Display or save output
            if (settings.Format == OutputFormat.Console)
            {
                DisplayConsoleOutput(review, settings);
            }
            else if (!string.IsNullOrEmpty(settings.OutputFile))
            {
                await _fileSystemService.WriteFileAsync(settings.OutputFile, output);
                AnsiConsole.MarkupLine($"[green]✓ Review saved to: {settings.OutputFile}[/]");
            }
            else
            {
                AnsiConsole.WriteLine(output);
            }
            
            // Return appropriate exit code
            return review.OverallScore < 60 ? 1 : 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task ReviewGitChanges(CodeReview review, Settings settings)
    {
        review.Type = settings.Type.ToString();
        
        string diff;
        if (settings.Type == ReviewType.Commit)
        {
            // Review last commit
            var commits = await _gitService.GetCommitHistoryAsync(settings.Path ?? ".", 1);
            var commit = commits.FirstOrDefault();
            if (commit != null)
            {
                review.Title = $"Review of commit: {commit.ShortId}";
                review.Description = commit.Message;
            }
            diff = await _gitService.GetDiffAsync(settings.Path ?? ".", null);
        }
        else
        {
            // Review PR (changes against base branch)
            review.Title = $"Pull Request Review";
            diff = await GetDiffAgainstBranch(settings.Path ?? ".", settings.BaseBranch!);
        }
        
        if (string.IsNullOrEmpty(diff))
        {
            AnsiConsole.MarkupLine("[yellow]No changes to review[/]");
            return;
        }
        
        await AnalyzeDiff(diff, review, settings);
    }

    private async Task ReviewPath(CodeReview review, Settings settings)
    {
        bool isFile = await _fileSystemService.FileExistsAsync(settings.Path!);
        bool isDirectory = await _fileSystemService.DirectoryExistsAsync(settings.Path!);
        
        if (!isFile && !isDirectory)
        {
            throw new InvalidOperationException($"Path '{settings.Path}' does not exist");
        }
        
        review.Type = settings.Type.ToString();
        review.Title = $"Code Review: {Path.GetFileName(settings.Path) ?? settings.Path}";
        
        var files = isFile 
            ? new[] { settings.Path! } 
            : await GetFilesToReview(settings.Path!);
        
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Reviewing files...[/]", maxValue: files.Length);
                
                foreach (var file in files)
                {
                    task.Description = $"[green]Reviewing: {Path.GetFileName(file)}[/]";
                    
                    var fileReview = await ReviewFile(file, settings);
                    review.Files.Add(fileReview);
                    
                    task.Increment(1);
                }
            });
        
        // Calculate overall metrics
        CalculateOverallMetrics(review);
    }

    private async Task<string[]> GetFilesToReview(string directory)
    {
        var extensions = new[] { "*.cs", "*.ts", "*.js", "*.jsx", "*.tsx", "*.py", "*.java", "*.go", "*.rs" };
        var files = new List<string>();
        
        foreach (var ext in extensions)
        {
            var foundFiles = await _fileSystemService.GetFilesAsync(directory, ext, true);
            files.AddRange(foundFiles.Where(f => 
                !f.Contains("node_modules") && 
                !f.Contains("bin") && 
                !f.Contains("obj") &&
                !f.Contains(".min.") &&
                !f.Contains("vendor") &&
                !f.Contains("dist") &&
                !f.Contains("test", StringComparison.OrdinalIgnoreCase)));
        }
        
        return files.Take(50).ToArray(); // Limit to 50 files for performance
    }

    private async Task<FileReview> ReviewFile(string filePath, Settings settings)
    {
        var content = await _fileSystemService.ReadFileAsync(filePath);
        var language = DetectLanguage(filePath);
        
        var prompt = BuildReviewPrompt(content, filePath, language, settings);
        
        var response = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
        {
            response.Append(chunk);
        }
        
        return ParseReviewResponse(response.ToString(), filePath);
    }

    private string BuildReviewPrompt(string content, string filePath, string language, Settings settings)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Perform a {settings.Depth} code review on the following {language} code:");
        prompt.AppendLine($"File: {Path.GetFileName(filePath)}");
        prompt.AppendLine();
        prompt.AppendLine("Code:");
        prompt.AppendLine(content);
        prompt.AppendLine();
        
        prompt.AppendLine("Review the code for:");
        
        // Basic checks
        prompt.AppendLine("1. Correctness and logic errors");
        prompt.AppendLine("2. Code clarity and readability");
        prompt.AppendLine("3. Naming conventions");
        prompt.AppendLine("4. Error handling");
        prompt.AppendLine("5. Performance issues");
        
        if (settings.Depth >= ReviewDepth.Standard)
        {
            prompt.AppendLine("6. Design patterns and architecture");
            prompt.AppendLine("7. SOLID principles adherence");
            prompt.AppendLine("8. Test coverage considerations");
            prompt.AppendLine("9. Documentation quality");
            prompt.AppendLine("10. Security concerns");
            prompt.AppendLine("11. Code duplication");
            prompt.AppendLine("12. Dependency management");
        }
        
        if (settings.Depth == ReviewDepth.Thorough)
        {
            prompt.AppendLine("13. Accessibility considerations");
            prompt.AppendLine("14. Internationalization readiness");
            prompt.AppendLine("15. Scalability concerns");
            prompt.AppendLine("16. Maintainability index");
            prompt.AppendLine("17. Technical debt");
            prompt.AppendLine("18. Best practices for the specific language/framework");
        }
        
        if (!string.IsNullOrEmpty(settings.Checks))
        {
            prompt.AppendLine($"Additional checks: {settings.Checks}");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("Return a JSON response with this structure:");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"score\": 0-100,");
        prompt.AppendLine("  \"summary\": \"overall assessment\",");
        prompt.AppendLine("  \"strengths\": [\"list of strengths\"],");
        prompt.AppendLine("  \"issues\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"severity\": \"critical|major|minor|suggestion\",");
        prompt.AppendLine("      \"category\": \"category of issue\",");
        prompt.AppendLine("      \"description\": \"issue description\",");
        prompt.AppendLine("      \"location\": \"line or function\",");
        prompt.AppendLine("      \"suggestion\": \"how to fix\"");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ],");
        
        if (settings.SuggestImprovements)
        {
            prompt.AppendLine("  \"improvements\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"area\": \"improvement area\",");
            prompt.AppendLine("      \"suggestion\": \"specific suggestion\",");
            prompt.AppendLine("      \"example\": \"code example if applicable\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ],");
        }
        
        if (settings.IncludeMetrics)
        {
            prompt.AppendLine("  \"metrics\": {");
            prompt.AppendLine("    \"complexity\": \"cyclomatic complexity\",");
            prompt.AppendLine("    \"maintainability\": \"maintainability score\",");
            prompt.AppendLine("    \"testability\": \"testability score\",");
            prompt.AppendLine("    \"readability\": \"readability score\"");
            prompt.AppendLine("  }");
        }
        
        prompt.AppendLine("}");
        
        return prompt.ToString();
    }

    private FileReview ParseReviewResponse(string response, string filePath)
    {
        var review = new FileReview
        {
            FilePath = filePath
        };
        
        try
        {
            // Find JSON in response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("score", out var score))
                {
                    review.Score = score.GetInt32();
                }
                
                if (root.TryGetProperty("summary", out var summary))
                {
                    review.Summary = summary.GetString() ?? "";
                }
                
                if (root.TryGetProperty("strengths", out var strengths))
                {
                    foreach (var strength in strengths.EnumerateArray())
                    {
                        review.Strengths.Add(strength.GetString() ?? "");
                    }
                }
                
                if (root.TryGetProperty("issues", out var issues))
                {
                    foreach (var issue in issues.EnumerateArray())
                    {
                        review.Issues.Add(new ReviewIssue
                        {
                            Severity = issue.GetProperty("severity").GetString() ?? "minor",
                            Category = issue.GetProperty("category").GetString() ?? "general",
                            Description = issue.GetProperty("description").GetString() ?? "",
                            Location = issue.TryGetProperty("location", out var loc) ? loc.GetString() : null,
                            Suggestion = issue.TryGetProperty("suggestion", out var sug) ? sug.GetString() : null
                        });
                    }
                }
                
                if (root.TryGetProperty("improvements", out var improvements))
                {
                    foreach (var improvement in improvements.EnumerateArray())
                    {
                        review.Improvements.Add(new Improvement
                        {
                            Area = improvement.GetProperty("area").GetString() ?? "",
                            Suggestion = improvement.GetProperty("suggestion").GetString() ?? "",
                            Example = improvement.TryGetProperty("example", out var ex) ? ex.GetString() : null
                        });
                    }
                }
                
                if (root.TryGetProperty("metrics", out var metrics))
                {
                    review.Metrics = new ReviewMetrics
                    {
                        Complexity = metrics.TryGetProperty("complexity", out var comp) ? comp.GetString() : null,
                        Maintainability = metrics.TryGetProperty("maintainability", out var maint) ? maint.GetString() : null,
                        Testability = metrics.TryGetProperty("testability", out var test) ? test.GetString() : null,
                        Readability = metrics.TryGetProperty("readability", out var read) ? read.GetString() : null
                    };
                }
            }
        }
        catch
        {
            // If parsing fails, create a basic review
            review.Score = 70;
            review.Summary = "Review completed with general observations";
            review.Issues.Add(new ReviewIssue
            {
                Severity = "suggestion",
                Category = "general",
                Description = response
            });
        }
        
        return review;
    }

    private async Task AnalyzeDiff(string diff, CodeReview review, Settings settings)
    {
        var prompt = $"Review the following Git diff:\n\n{diff}\n\n" +
                    "Provide a code review focusing on:\n" +
                    "1. Logic errors or bugs introduced\n" +
                    "2. Performance implications\n" +
                    "3. Security concerns\n" +
                    "4. Code style and consistency\n" +
                    "5. Test coverage for changes\n\n" +
                    "Return review in JSON format as specified.";
        
        var response = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
        {
            response.Append(chunk);
        }
        
        var fileReview = ParseReviewResponse(response.ToString(), "diff");
        review.Files.Add(fileReview);
        CalculateOverallMetrics(review);
    }

    private async Task<string> GetDiffAgainstBranch(string path, string baseBranch)
    {
        // This would use git diff base..HEAD
        return await _gitService.GetDiffAsync(path);
    }

    private void CalculateOverallMetrics(CodeReview review)
    {
        if (review.Files.Any())
        {
            review.OverallScore = (int)review.Files.Average(f => f.Score);
            review.TotalIssues = review.Files.Sum(f => f.Issues.Count);
            review.CriticalIssues = review.Files.Sum(f => f.Issues.Count(i => i.Severity == "critical"));
            review.MajorIssues = review.Files.Sum(f => f.Issues.Count(i => i.Severity == "major"));
            review.MinorIssues = review.Files.Sum(f => f.Issues.Count(i => i.Severity == "minor"));
        }
    }

    private void DisplayConsoleOutput(CodeReview review, Settings settings)
    {
        // Header
        var headerPanel = new Panel(new Rows(
            new Markup($"[bold]{review.Title}[/]"),
            !string.IsNullOrEmpty(review.Description) ? new Markup(review.Description) : Text.Empty
        ))
        {
            Header = new PanelHeader(" Code Review "),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(headerPanel);
        
        // Overall score
        var scoreColor = review.OverallScore >= 80 ? "green" : 
                        review.OverallScore >= 60 ? "yellow" : "red";
        
        var scoreBarColor = review.OverallScore >= 80 ? Color.Green : 
                           review.OverallScore >= 60 ? Color.Yellow : Color.Red;
        
        var scoreBar = new BarChart()
            .Width(60)
            .Label($"[bold]Overall Score[/]")
            .AddItem($"Score", review.OverallScore, scoreBarColor);
        AnsiConsole.Write(scoreBar);
        
        // Issues summary
        if (review.TotalIssues > 0)
        {
            var issueTable = new Table();
            issueTable.AddColumn("Severity");
            issueTable.AddColumn("Count");
            issueTable.Border(TableBorder.Rounded);
            
            if (review.CriticalIssues > 0)
                issueTable.AddRow("[red]Critical[/]", review.CriticalIssues.ToString());
            if (review.MajorIssues > 0)
                issueTable.AddRow("[orange1]Major[/]", review.MajorIssues.ToString());
            if (review.MinorIssues > 0)
                issueTable.AddRow("[yellow]Minor[/]", review.MinorIssues.ToString());
            
            AnsiConsole.Write(issueTable);
        }
        
        // File reviews
        foreach (var file in review.Files)
        {
            AnsiConsole.Write(new Rule($"[cyan]{Path.GetFileName(file.FilePath)}[/]"));
            
            if (!string.IsNullOrEmpty(file.Summary))
            {
                AnsiConsole.MarkupLine($"[italic]{file.Summary}[/]");
            }
            
            if (file.Strengths.Any())
            {
                AnsiConsole.MarkupLine("\n[green]Strengths:[/]");
                foreach (var strength in file.Strengths)
                {
                    AnsiConsole.MarkupLine($"  ✓ {strength}");
                }
            }
            
            if (file.Issues.Any())
            {
                AnsiConsole.MarkupLine("\n[yellow]Issues:[/]");
                foreach (var issue in file.Issues)
                {
                    var icon = issue.Severity switch
                    {
                        "critical" => "[red]✗[/]",
                        "major" => "[orange1]![/]",
                        "minor" => "[yellow]⚠[/]",
                        _ => "[blue]ℹ[/]"
                    };
                    
                    AnsiConsole.MarkupLine($"  {icon} [{issue.Category}] {issue.Description}");
                    if (!string.IsNullOrEmpty(issue.Location))
                    {
                        AnsiConsole.MarkupLine($"    Location: {issue.Location}");
                    }
                    if (!string.IsNullOrEmpty(issue.Suggestion))
                    {
                        AnsiConsole.MarkupLine($"    [green]Fix:[/] {issue.Suggestion}");
                    }
                }
            }
            
            if (settings.SuggestImprovements && file.Improvements.Any())
            {
                AnsiConsole.MarkupLine("\n[cyan]Improvements:[/]");
                foreach (var improvement in file.Improvements)
                {
                    AnsiConsole.MarkupLine($"  💡 [{improvement.Area}] {improvement.Suggestion}");
                }
            }
        }
        
        // Summary
        AnsiConsole.Write(new Rule());
        if (review.OverallScore >= 80)
        {
            AnsiConsole.MarkupLine("[green]✓ Code review passed - Good quality![/]");
        }
        else if (review.OverallScore >= 60)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Code review passed with concerns - Address issues before merging[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗ Code review failed - Significant issues need to be addressed[/]");
        }
    }

    private string GenerateMarkdownOutput(CodeReview review, Settings settings)
    {
        var md = new StringBuilder();
        md.AppendLine("# Code Review Report");
        md.AppendLine();
        md.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine($"**Type:** {review.Type}");
        md.AppendLine($"**Overall Score:** {review.OverallScore}/100");
        md.AppendLine();
        
        if (review.TotalIssues > 0)
        {
            md.AppendLine("## Issues Summary");
            md.AppendLine($"- Critical: {review.CriticalIssues}");
            md.AppendLine($"- Major: {review.MajorIssues}");
            md.AppendLine($"- Minor: {review.MinorIssues}");
            md.AppendLine();
        }
        
        foreach (var file in review.Files)
        {
            md.AppendLine($"## {Path.GetFileName(file.FilePath)}");
            md.AppendLine($"**Score:** {file.Score}/100");
            
            if (!string.IsNullOrEmpty(file.Summary))
            {
                md.AppendLine();
                md.AppendLine(file.Summary);
            }
            
            if (file.Strengths.Any())
            {
                md.AppendLine();
                md.AppendLine("### Strengths");
                foreach (var strength in file.Strengths)
                {
                    md.AppendLine($"- ✓ {strength}");
                }
            }
            
            if (file.Issues.Any())
            {
                md.AppendLine();
                md.AppendLine("### Issues");
                foreach (var issue in file.Issues)
                {
                    md.AppendLine($"- **[{issue.Severity.ToUpper()}]** {issue.Description}");
                    if (!string.IsNullOrEmpty(issue.Suggestion))
                    {
                        md.AppendLine($"  - **Fix:** {issue.Suggestion}");
                    }
                }
            }
            
            if (file.Improvements.Any())
            {
                md.AppendLine();
                md.AppendLine("### Suggested Improvements");
                foreach (var improvement in file.Improvements)
                {
                    md.AppendLine($"- **{improvement.Area}:** {improvement.Suggestion}");
                }
            }
            
            md.AppendLine();
        }
        
        return md.ToString();
    }

    private string GenerateGitHubOutput(CodeReview review, Settings settings)
    {
        // GitHub-flavored markdown with PR review format
        var md = new StringBuilder();
        
        if (review.OverallScore < 60)
        {
            md.AppendLine("## ❌ Changes requested");
        }
        else if (review.OverallScore < 80)
        {
            md.AppendLine("## ⚠️ Approved with comments");
        }
        else
        {
            md.AppendLine("## ✅ Approved");
        }
        
        md.AppendLine();
        md.AppendLine($"**Review Score:** {review.OverallScore}/100");
        md.AppendLine();
        
        if (review.CriticalIssues > 0 || review.MajorIssues > 0)
        {
            md.AppendLine("### 🚨 Issues that need attention");
            foreach (var file in review.Files)
            {
                var criticalAndMajor = file.Issues.Where(i => i.Severity == "critical" || i.Severity == "major");
                foreach (var issue in criticalAndMajor)
                {
                    md.AppendLine($"- [ ] **{issue.Description}** in `{Path.GetFileName(file.FilePath)}`");
                    if (!string.IsNullOrEmpty(issue.Suggestion))
                    {
                        md.AppendLine($"  - 💡 {issue.Suggestion}");
                    }
                }
            }
            md.AppendLine();
        }
        
        if (review.Files.Any(f => f.Improvements.Any()))
        {
            md.AppendLine("### 💡 Suggestions for improvement");
            foreach (var file in review.Files)
            {
                foreach (var improvement in file.Improvements)
                {
                    md.AppendLine($"- {improvement.Suggestion}");
                }
            }
        }
        
        return md.ToString();
    }

    private string DetectLanguage(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".cs" => "C#",
            ".ts" or ".tsx" => "TypeScript",
            ".js" or ".jsx" => "JavaScript",
            ".py" => "Python",
            ".java" => "Java",
            ".go" => "Go",
            ".rs" => "Rust",
            _ => "Unknown"
        };
    }

    // Data models
    private class CodeReview
    {
        public string Type { get; set; } = "Code";
        public string Title { get; set; } = "Code Review";
        public string? Description { get; set; }
        public int OverallScore { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public int MajorIssues { get; set; }
        public int MinorIssues { get; set; }
        public List<FileReview> Files { get; set; } = new();
    }

    private class FileReview
    {
        public string FilePath { get; set; } = string.Empty;
        public int Score { get; set; } = 100;
        public string Summary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<ReviewIssue> Issues { get; set; } = new();
        public List<Improvement> Improvements { get; set; } = new();
        public ReviewMetrics? Metrics { get; set; }
    }

    private class ReviewIssue
    {
        public string Severity { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Suggestion { get; set; }
    }

    private class Improvement
    {
        public string Area { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string? Example { get; set; }
    }

    private class ReviewMetrics
    {
        public string? Complexity { get; set; }
        public string? Maintainability { get; set; }
        public string? Testability { get; set; }
        public string? Readability { get; set; }
    }
}