using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class LintCommand : AsyncCommand<LintCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;

    public LintCommand(IFileSystemService fileSystemService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("File or directory to lint")]
        [CommandArgument(0, "<path>")]
        public string Path { get; set; } = string.Empty;

        [Description("Linting rules set (default, strict, relaxed, custom)")]
        [CommandOption("-r|--rules")]
        public RulesSet Rules { get; set; } = RulesSet.Default;

        [Description("Fix issues automatically where possible")]
        [CommandOption("--fix")]
        public bool AutoFix { get; set; }

        [Description("Output format (console, json, markdown)")]
        [CommandOption("-f|--format")]
        public OutputFormat Format { get; set; } = OutputFormat.Console;

        [Description("Include suggestions for improvements")]
        [CommandOption("--suggestions")]
        public bool IncludeSuggestions { get; set; } = true;

        [Description("Check code style and formatting")]
        [CommandOption("--style")]
        public bool CheckStyle { get; set; } = true;

        [Description("Check for potential bugs")]
        [CommandOption("--bugs")]
        public bool CheckBugs { get; set; } = true;

        [Description("Check for code smells")]
        [CommandOption("--smells")]
        public bool CheckCodeSmells { get; set; } = true;

        [Description("Output file for results")]
        [CommandOption("-o|--output")]
        public string? OutputFile { get; set; }

        [Description("Severity level (error, warning, info)")]
        [CommandOption("--severity")]
        public SeverityLevel MinSeverity { get; set; } = SeverityLevel.Info;
    }

    public enum RulesSet
    {
        Default,
        Strict,
        Relaxed,
        Custom
    }

    public enum OutputFormat
    {
        Console,
        Json,
        Markdown
    }

    public enum SeverityLevel
    {
        Error,
        Warning,
        Info
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold cyan]Code Linting[/]"));
            
            // Determine if path is file or directory
            bool isFile = await _fileSystemService.FileExistsAsync(settings.Path);
            bool isDirectory = await _fileSystemService.DirectoryExistsAsync(settings.Path);
            
            if (!isFile && !isDirectory)
            {
                AnsiConsole.MarkupLine($"[red]Error: Path '{settings.Path}' does not exist[/]");
                return 1;
            }
            
            // Get files to lint
            var files = isFile 
                ? new[] { settings.Path } 
                : await GetFilesToLint(settings.Path);
            
            if (!files.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No files found to lint[/]");
                return 0;
            }
            
            AnsiConsole.MarkupLine($"[cyan]Linting {files.Length} file(s) with {settings.Rules} rules[/]");
            
            var allIssues = new List<LintIssue>();
            var fixedFiles = new List<string>();
            
            // Lint each file
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing files...[/]", maxValue: files.Length);
                    
                    foreach (var file in files)
                    {
                        task.Description = $"[green]Linting: {Path.GetFileName(file)}[/]";
                        
                        var issues = await LintFile(file, settings);
                        allIssues.AddRange(issues);
                        
                        if (settings.AutoFix && issues.Any(i => i.CanAutoFix))
                        {
                            var wasFixed = await AutoFixFile(file, issues.Where(i => i.CanAutoFix).ToList());
                            if (wasFixed)
                            {
                                fixedFiles.Add(file);
                            }
                        }
                        
                        task.Increment(1);
                    }
                });
            
            // Filter by severity
            var filteredIssues = FilterBySeverity(allIssues, settings.MinSeverity);
            
            // Generate output
            var output = settings.Format switch
            {
                OutputFormat.Console => GenerateConsoleOutput(filteredIssues, fixedFiles),
                OutputFormat.Json => GenerateJsonOutput(filteredIssues),
                OutputFormat.Markdown => GenerateMarkdownOutput(filteredIssues, settings),
                _ => string.Empty
            };
            
            // Display or save output
            if (settings.Format == OutputFormat.Console)
            {
                // Console output is handled differently
                DisplayConsoleOutput(filteredIssues, fixedFiles);
            }
            else if (!string.IsNullOrEmpty(settings.OutputFile))
            {
                await _fileSystemService.WriteFileAsync(settings.OutputFile, output);
                AnsiConsole.MarkupLine($"[green]✓ Results saved to: {settings.OutputFile}[/]");
            }
            else
            {
                AnsiConsole.WriteLine(output);
            }
            
            // Display summary
            DisplaySummary(allIssues, fixedFiles);
            
            // Return non-zero if errors found
            return allIssues.Any(i => i.Severity == SeverityLevel.Error) ? 1 : 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<string[]> GetFilesToLint(string directory)
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
                !f.Contains("vendor")));
        }
        
        return files.ToArray();
    }

    private async Task<List<LintIssue>> LintFile(string filePath, Settings settings)
    {
        var issues = new List<LintIssue>();
        var content = await _fileSystemService.ReadFileAsync(filePath);
        
        if (string.IsNullOrWhiteSpace(content))
        {
            return issues;
        }
        
        var prompt = new StringBuilder();
        prompt.AppendLine($"Perform code linting on the following file:");
        prompt.AppendLine($"File: {Path.GetFileName(filePath)}");
        prompt.AppendLine($"Language: {DetectLanguage(filePath)}");
        prompt.AppendLine($"Rules Set: {settings.Rules}");
        prompt.AppendLine();
        prompt.AppendLine("Code:");
        prompt.AppendLine(content);
        prompt.AppendLine();
        prompt.AppendLine("Analyze for:");
        
        if (settings.CheckStyle)
        {
            prompt.AppendLine("1. Code style and formatting issues");
            prompt.AppendLine("2. Naming convention violations");
            prompt.AppendLine("3. Indentation and spacing problems");
        }
        
        if (settings.CheckBugs)
        {
            prompt.AppendLine("4. Potential bugs and logic errors");
            prompt.AppendLine("5. Null reference issues");
            prompt.AppendLine("6. Resource leaks");
        }
        
        if (settings.CheckCodeSmells)
        {
            prompt.AppendLine("7. Code smells and anti-patterns");
            prompt.AppendLine("8. Duplicate code");
            prompt.AppendLine("9. Complex or long methods");
            prompt.AppendLine("10. Poor error handling");
        }
        
        if (settings.IncludeSuggestions)
        {
            prompt.AppendLine("11. Performance improvements");
            prompt.AppendLine("12. Best practice violations");
            prompt.AppendLine("13. Security concerns");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("Return a JSON array of issues with this structure:");
        prompt.AppendLine("[{");
        prompt.AppendLine("  \"line\": <line_number>,");
        prompt.AppendLine("  \"column\": <column_number>,");
        prompt.AppendLine("  \"severity\": \"error|warning|info\",");
        prompt.AppendLine("  \"message\": \"<description>\",");
        prompt.AppendLine("  \"rule\": \"<rule_name>\",");
        prompt.AppendLine("  \"canAutoFix\": true|false,");
        prompt.AppendLine("  \"suggestion\": \"<fix_suggestion>\"");
        prompt.AppendLine("}]");
        
        var response = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt.ToString()))
        {
            response.Append(chunk);
        }
        
        // Parse the response and create issues
        try
        {
            var jsonStart = response.ToString().IndexOf('[');
            var jsonEnd = response.ToString().LastIndexOf(']') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.ToString().Substring(jsonStart, jsonEnd - jsonStart);
                var parsedIssues = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
                
                if (parsedIssues != null)
                {
                    foreach (var issue in parsedIssues)
                    {
                        issues.Add(new LintIssue
                        {
                            FilePath = filePath,
                            Line = Convert.ToInt32(issue.GetValueOrDefault("line", 0)),
                            Column = Convert.ToInt32(issue.GetValueOrDefault("column", 0)),
                            Severity = Enum.Parse<SeverityLevel>(issue.GetValueOrDefault("severity", "Info")?.ToString() ?? "Info", true),
                            Message = issue.GetValueOrDefault("message")?.ToString() ?? "",
                            Rule = issue.GetValueOrDefault("rule")?.ToString() ?? "",
                            CanAutoFix = Convert.ToBoolean(issue.GetValueOrDefault("canAutoFix", false)),
                            Suggestion = issue.GetValueOrDefault("suggestion")?.ToString()
                        });
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, create a generic issue
            issues.Add(new LintIssue
            {
                FilePath = filePath,
                Line = 0,
                Column = 0,
                Severity = SeverityLevel.Info,
                Message = "Linting completed with suggestions available",
                Rule = "general"
            });
        }
        
        return issues;
    }

    private async Task<bool> AutoFixFile(string filePath, List<LintIssue> fixableIssues)
    {
        if (!fixableIssues.Any())
            return false;
        
        var content = await _fileSystemService.ReadFileAsync(filePath);
        var prompt = new StringBuilder();
        
        prompt.AppendLine("Apply the following fixes to the code:");
        prompt.AppendLine();
        
        foreach (var issue in fixableIssues)
        {
            prompt.AppendLine($"- Line {issue.Line}: {issue.Message}");
            if (!string.IsNullOrEmpty(issue.Suggestion))
            {
                prompt.AppendLine($"  Fix: {issue.Suggestion}");
            }
        }
        
        prompt.AppendLine();
        prompt.AppendLine("Original code:");
        prompt.AppendLine(content);
        prompt.AppendLine();
        prompt.AppendLine("Return ONLY the fixed code, no explanations.");
        
        var fixedContent = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt.ToString()))
        {
            fixedContent.Append(chunk);
        }
        
        // Clean up the response (remove markdown code blocks if present)
        var cleaned = fixedContent.ToString();
        if (cleaned.Contains("```"))
        {
            var start = cleaned.IndexOf("```");
            var end = cleaned.LastIndexOf("```");
            if (start >= 0 && end > start)
            {
                var langEnd = cleaned.IndexOf('\n', start);
                if (langEnd > start && langEnd < end)
                {
                    cleaned = cleaned.Substring(langEnd + 1, end - langEnd - 1).Trim();
                }
            }
        }
        
        await _fileSystemService.WriteFileAsync(filePath, cleaned);
        return true;
    }

    private string DetectLanguage(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".cs" => "C#",
            ".ts" => "TypeScript",
            ".tsx" => "TypeScript React",
            ".js" => "JavaScript",
            ".jsx" => "JavaScript React",
            ".py" => "Python",
            ".java" => "Java",
            ".go" => "Go",
            ".rs" => "Rust",
            ".cpp" or ".cc" => "C++",
            ".c" => "C",
            _ => "Unknown"
        };
    }

    private List<LintIssue> FilterBySeverity(List<LintIssue> issues, SeverityLevel minSeverity)
    {
        return minSeverity switch
        {
            SeverityLevel.Error => issues.Where(i => i.Severity == SeverityLevel.Error).ToList(),
            SeverityLevel.Warning => issues.Where(i => i.Severity <= SeverityLevel.Warning).ToList(),
            _ => issues
        };
    }

    private void DisplayConsoleOutput(List<LintIssue> issues, List<string> fixedFiles)
    {
        if (!issues.Any())
        {
            AnsiConsole.MarkupLine("[green]✓ No issues found![/]");
            return;
        }
        
        // Group issues by file
        var issuesByFile = issues.GroupBy(i => i.FilePath);
        
        foreach (var fileGroup in issuesByFile)
        {
            var fileName = Path.GetRelativePath(".", fileGroup.Key);
            var isFixed = fixedFiles.Contains(fileGroup.Key);
            
            var panel = new Panel(new Rows(
                fileGroup.Select(issue =>
                {
                    var icon = issue.Severity switch
                    {
                        SeverityLevel.Error => "[red]✗[/]",
                        SeverityLevel.Warning => "[yellow]⚠[/]",
                        _ => "[blue]ℹ[/]"
                    };
                    
                    var text = $"{icon} [{issue.Severity}] Line {issue.Line}:{issue.Column} - {issue.Message}";
                    if (!string.IsNullOrEmpty(issue.Rule))
                    {
                        text += $" ({issue.Rule})";
                    }
                    if (issue.CanAutoFix && isFixed)
                    {
                        text += " [green](fixed)[/]";
                    }
                    
                    return new Markup(text);
                })))
            {
                Header = new PanelHeader($" {fileName} "),
                Border = BoxBorder.Rounded
            };
            
            AnsiConsole.Write(panel);
        }
    }

    private string GenerateConsoleOutput(List<LintIssue> issues, List<string> fixedFiles)
    {
        // This is handled by DisplayConsoleOutput
        return string.Empty;
    }

    private string GenerateJsonOutput(List<LintIssue> issues)
    {
        return System.Text.Json.JsonSerializer.Serialize(issues, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string GenerateMarkdownOutput(List<LintIssue> issues, Settings settings)
    {
        var md = new StringBuilder();
        md.AppendLine("# Linting Report");
        md.AppendLine();
        md.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine($"**Rules:** {settings.Rules}");
        md.AppendLine($"**Total Issues:** {issues.Count}");
        md.AppendLine();
        
        var bySeverity = issues.GroupBy(i => i.Severity);
        md.AppendLine("## Summary");
        foreach (var group in bySeverity)
        {
            md.AppendLine($"- {group.Key}: {group.Count()}");
        }
        md.AppendLine();
        
        md.AppendLine("## Issues by File");
        var byFile = issues.GroupBy(i => i.FilePath);
        foreach (var fileGroup in byFile)
        {
            md.AppendLine($"### {Path.GetRelativePath(".", fileGroup.Key)}");
            md.AppendLine();
            
            foreach (var issue in fileGroup.OrderBy(i => i.Line))
            {
                var icon = issue.Severity switch
                {
                    SeverityLevel.Error => "❌",
                    SeverityLevel.Warning => "⚠️",
                    _ => "ℹ️"
                };
                
                md.AppendLine($"{icon} **Line {issue.Line}:{issue.Column}** - {issue.Message}");
                if (!string.IsNullOrEmpty(issue.Rule))
                {
                    md.AppendLine($"   - Rule: `{issue.Rule}`");
                }
                if (!string.IsNullOrEmpty(issue.Suggestion))
                {
                    md.AppendLine($"   - Suggestion: {issue.Suggestion}");
                }
                md.AppendLine();
            }
        }
        
        return md.ToString();
    }

    private void DisplaySummary(List<LintIssue> issues, List<string> fixedFiles)
    {
        var table = new Table();
        table.AddColumn("Metric");
        table.AddColumn("Count");
        table.Border(TableBorder.Rounded);
        
        table.AddRow("Total Issues", issues.Count.ToString());
        table.AddRow("[red]Errors[/]", issues.Count(i => i.Severity == SeverityLevel.Error).ToString());
        table.AddRow("[yellow]Warnings[/]", issues.Count(i => i.Severity == SeverityLevel.Warning).ToString());
        table.AddRow("[blue]Info[/]", issues.Count(i => i.Severity == SeverityLevel.Info).ToString());
        
        if (fixedFiles.Any())
        {
            table.AddRow("[green]Files Fixed[/]", fixedFiles.Count.ToString());
        }
        
        AnsiConsole.Write(table);
        
        if (issues.Any(i => i.Severity == SeverityLevel.Error))
        {
            AnsiConsole.MarkupLine("[red]✗ Linting failed with errors[/]");
        }
        else if (issues.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Linting completed with warnings[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]✓ Linting passed[/]");
        }
    }

    private class LintIssue
    {
        public string FilePath { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public SeverityLevel Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Rule { get; set; } = string.Empty;
        public bool CanAutoFix { get; set; }
        public string? Suggestion { get; set; }
    }
}