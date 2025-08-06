using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class OptimizeCommand : AsyncCommand<OptimizeCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;

    public OptimizeCommand(IFileSystemService fileSystemService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("File or directory to optimize")]
        [CommandArgument(0, "<path>")]
        public string Path { get; set; } = string.Empty;

        [Description("Optimization focus (performance, memory, readability, all)")]
        [CommandOption("-f|--focus")]
        public OptimizationFocus Focus { get; set; } = OptimizationFocus.All;

        [Description("Apply optimizations automatically")]
        [CommandOption("--apply")]
        public bool AutoApply { get; set; }

        [Description("Create backup before applying changes")]
        [CommandOption("--backup")]
        public bool CreateBackup { get; set; } = true;

        [Description("Target language version/standard")]
        [CommandOption("--target")]
        public string? TargetVersion { get; set; }

        [Description("Aggressive optimization mode")]
        [CommandOption("--aggressive")]
        public bool Aggressive { get; set; }

        [Description("Include refactoring suggestions")]
        [CommandOption("--refactor")]
        public bool IncludeRefactoring { get; set; } = true;

        [Description("Output file for optimization report")]
        [CommandOption("-o|--output")]
        public string? OutputFile { get; set; }
    }

    public enum OptimizationFocus
    {
        Performance,
        Memory,
        Readability,
        All
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold cyan]Code Optimization[/]"));
            
            // Determine if path is file or directory
            bool isFile = await _fileSystemService.FileExistsAsync(settings.Path);
            bool isDirectory = await _fileSystemService.DirectoryExistsAsync(settings.Path);
            
            if (!isFile && !isDirectory)
            {
                AnsiConsole.MarkupLine($"[red]Error: Path '{settings.Path}' does not exist[/]");
                return 1;
            }
            
            // Get files to optimize
            var files = isFile 
                ? new[] { settings.Path } 
                : await GetFilesToOptimize(settings.Path);
            
            if (!files.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No files found to optimize[/]");
                return 0;
            }
            
            AnsiConsole.MarkupLine($"[cyan]Optimizing {files.Length} file(s) with focus: {settings.Focus}[/]");
            
            var optimizations = new List<OptimizationResult>();
            var appliedFiles = new List<string>();
            
            // Analyze and optimize each file
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing files...[/]", maxValue: files.Length);
                    
                    foreach (var file in files)
                    {
                        task.Description = $"[green]Optimizing: {Path.GetFileName(file)}[/]";
                        
                        // Create backup if requested
                        if (settings.CreateBackup && settings.AutoApply)
                        {
                            await CreateBackupAsync(file);
                        }
                        
                        var result = await OptimizeFile(file, settings);
                        if (result != null)
                        {
                            optimizations.Add(result);
                            
                            if (settings.AutoApply && result.HasOptimizations)
                            {
                                await ApplyOptimizations(file, result);
                                appliedFiles.Add(file);
                            }
                        }
                        
                        task.Increment(1);
                    }
                });
            
            // Display results
            DisplayOptimizationResults(optimizations, appliedFiles);
            
            // Generate report if requested
            if (!string.IsNullOrEmpty(settings.OutputFile))
            {
                var report = GenerateOptimizationReport(optimizations, settings);
                await _fileSystemService.WriteFileAsync(settings.OutputFile, report);
                AnsiConsole.MarkupLine($"[green]✓ Report saved to: {settings.OutputFile}[/]");
            }
            
            return optimizations.Any(o => o.HasOptimizations) ? 0 : 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<string[]> GetFilesToOptimize(string directory)
    {
        var extensions = new[] { "*.cs", "*.ts", "*.js", "*.jsx", "*.tsx", "*.py", "*.java", "*.go", "*.rs", "*.cpp", "*.c" };
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
                !f.Contains("dist")));
        }
        
        return files.ToArray();
    }

    private async Task<OptimizationResult?> OptimizeFile(string filePath, Settings settings)
    {
        var content = await _fileSystemService.ReadFileAsync(filePath);
        
        if (string.IsNullOrWhiteSpace(content) || content.Length < 50)
        {
            return null;
        }
        
        var language = DetectLanguage(filePath);
        var prompt = BuildOptimizationPrompt(content, filePath, language, settings);
        
        var response = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
        {
            response.Append(chunk);
        }
        
        return ParseOptimizationResponse(response.ToString(), filePath, content);
    }

    private string BuildOptimizationPrompt(string content, string filePath, string language, Settings settings)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Analyze and optimize the following {language} code:");
        prompt.AppendLine($"File: {Path.GetFileName(filePath)}");
        
        if (!string.IsNullOrEmpty(settings.TargetVersion))
        {
            prompt.AppendLine($"Target Version: {settings.TargetVersion}");
        }
        
        prompt.AppendLine($"Optimization Focus: {settings.Focus}");
        prompt.AppendLine($"Aggressive Mode: {settings.Aggressive}");
        prompt.AppendLine();
        prompt.AppendLine("Code:");
        prompt.AppendLine(content);
        prompt.AppendLine();
        prompt.AppendLine("Analyze for:");
        
        if (settings.Focus == OptimizationFocus.Performance || settings.Focus == OptimizationFocus.All)
        {
            prompt.AppendLine("1. Algorithm improvements and complexity reduction");
            prompt.AppendLine("2. Loop optimizations and vectorization opportunities");
            prompt.AppendLine("3. Unnecessary computations and redundant operations");
            prompt.AppendLine("4. Caching opportunities");
            prompt.AppendLine("5. Async/parallel processing opportunities");
        }
        
        if (settings.Focus == OptimizationFocus.Memory || settings.Focus == OptimizationFocus.All)
        {
            prompt.AppendLine("6. Memory allocation reduction");
            prompt.AppendLine("7. Object pooling opportunities");
            prompt.AppendLine("8. Memory leak detection");
            prompt.AppendLine("9. String concatenation optimizations");
            prompt.AppendLine("10. Collection size optimizations");
        }
        
        if (settings.Focus == OptimizationFocus.Readability || settings.Focus == OptimizationFocus.All)
        {
            prompt.AppendLine("11. Code simplification");
            prompt.AppendLine("12. Naming improvements");
            prompt.AppendLine("13. Extract method opportunities");
            prompt.AppendLine("14. Remove dead code");
            prompt.AppendLine("15. Reduce complexity and improve maintainability");
        }
        
        if (settings.IncludeRefactoring)
        {
            prompt.AppendLine("16. Design pattern applications");
            prompt.AppendLine("17. SOLID principle violations");
            prompt.AppendLine("18. Code duplication");
        }
        
        if (settings.Aggressive)
        {
            prompt.AppendLine("19. Micro-optimizations");
            prompt.AppendLine("20. Platform-specific optimizations");
            prompt.AppendLine("21. Compiler hints and attributes");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("Return a JSON response with this structure:");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"optimizations\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"type\": \"performance|memory|readability\",");
        prompt.AppendLine("      \"severity\": \"high|medium|low\",");
        prompt.AppendLine("      \"description\": \"description of optimization\",");
        prompt.AppendLine("      \"location\": \"line number or range\",");
        prompt.AppendLine("      \"original\": \"original code snippet\",");
        prompt.AppendLine("      \"optimized\": \"optimized code snippet\",");
        prompt.AppendLine("      \"impact\": \"expected impact\",");
        prompt.AppendLine("      \"explanation\": \"why this optimization helps\"");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ],");
        prompt.AppendLine("  \"metrics\": {");
        prompt.AppendLine("    \"complexity\": \"current cyclomatic complexity\",");
        prompt.AppendLine("    \"estimatedImprovement\": \"percentage improvement possible\"");
        prompt.AppendLine("  },");
        prompt.AppendLine("  \"optimizedCode\": \"full optimized code if applicable\"");
        prompt.AppendLine("}");
        
        return prompt.ToString();
    }

    private OptimizationResult ParseOptimizationResponse(string response, string filePath, string originalContent)
    {
        var result = new OptimizationResult
        {
            FilePath = filePath,
            OriginalContent = originalContent
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
                
                // Parse optimizations
                if (root.TryGetProperty("optimizations", out var optimizationsElement))
                {
                    foreach (var opt in optimizationsElement.EnumerateArray())
                    {
                        result.Optimizations.Add(new Optimization
                        {
                            Type = opt.GetProperty("type").GetString() ?? "general",
                            Severity = opt.GetProperty("severity").GetString() ?? "low",
                            Description = opt.GetProperty("description").GetString() ?? "",
                            Location = opt.GetProperty("location").GetString() ?? "",
                            Original = opt.TryGetProperty("original", out var orig) ? orig.GetString() ?? "" : "",
                            Optimized = opt.TryGetProperty("optimized", out var optim) ? optim.GetString() ?? "" : "",
                            Impact = opt.TryGetProperty("impact", out var impact) ? impact.GetString() ?? "" : "",
                            Explanation = opt.TryGetProperty("explanation", out var exp) ? exp.GetString() ?? "" : ""
                        });
                    }
                }
                
                // Parse optimized code
                if (root.TryGetProperty("optimizedCode", out var optimizedCode))
                {
                    result.OptimizedContent = optimizedCode.GetString();
                }
                
                // Parse metrics
                if (root.TryGetProperty("metrics", out var metrics))
                {
                    if (metrics.TryGetProperty("complexity", out var complexity))
                    {
                        result.Complexity = complexity.GetString();
                    }
                    if (metrics.TryGetProperty("estimatedImprovement", out var improvement))
                    {
                        result.EstimatedImprovement = improvement.GetString();
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, treat entire response as suggestion
            result.Optimizations.Add(new Optimization
            {
                Type = "general",
                Severity = "medium",
                Description = "Optimization suggestions available",
                Explanation = response
            });
        }
        
        return result;
    }

    private async Task CreateBackupAsync(string filePath)
    {
        var content = await _fileSystemService.ReadFileAsync(filePath);
        var backupPath = $"{filePath}.backup.{DateTime.Now:yyyyMMddHHmmss}";
        await _fileSystemService.WriteFileAsync(backupPath, content);
    }

    private async Task ApplyOptimizations(string filePath, OptimizationResult result)
    {
        if (!string.IsNullOrEmpty(result.OptimizedContent))
        {
            await _fileSystemService.WriteFileAsync(filePath, result.OptimizedContent);
        }
        else if (result.Optimizations.Any())
        {
            // Apply individual optimizations
            var content = result.OriginalContent;
            
            foreach (var opt in result.Optimizations.Where(o => !string.IsNullOrEmpty(o.Original) && !string.IsNullOrEmpty(o.Optimized)))
            {
                content = content.Replace(opt.Original, opt.Optimized);
            }
            
            if (content != result.OriginalContent)
            {
                await _fileSystemService.WriteFileAsync(filePath, content);
            }
        }
    }

    private void DisplayOptimizationResults(List<OptimizationResult> results, List<string> appliedFiles)
    {
        if (!results.Any(r => r.HasOptimizations))
        {
            AnsiConsole.MarkupLine("[green]✓ No optimizations needed - code is already optimal![/]");
            return;
        }
        
        // Group by file
        foreach (var result in results.Where(r => r.HasOptimizations))
        {
            var fileName = Path.GetRelativePath(".", result.FilePath);
            var wasApplied = appliedFiles.Contains(result.FilePath);
            
            var table = new Table();
            table.AddColumn("Type");
            table.AddColumn("Severity");
            table.AddColumn("Description");
            table.AddColumn("Impact");
            table.Border(TableBorder.Rounded);
            table.Title = new TableTitle($"{fileName} {(wasApplied ? "[green](applied)[/]" : "")}");
            
            foreach (var opt in result.Optimizations)
            {
                var severityColor = opt.Severity switch
                {
                    "high" => "red",
                    "medium" => "yellow",
                    _ => "blue"
                };
                
                table.AddRow(
                    opt.Type,
                    $"[{severityColor}]{opt.Severity}[/]",
                    opt.Description,
                    opt.Impact
                );
            }
            
            AnsiConsole.Write(table);
            
            if (!string.IsNullOrEmpty(result.EstimatedImprovement))
            {
                AnsiConsole.MarkupLine($"[cyan]Estimated improvement: {result.EstimatedImprovement}[/]");
            }
        }
        
        // Summary
        var totalOptimizations = results.Sum(r => r.Optimizations.Count);
        var highSeverity = results.Sum(r => r.Optimizations.Count(o => o.Severity == "high"));
        
        var summaryPanel = new Panel($"[bold]Optimization Summary[/]\n" +
                                    $"Total optimizations found: {totalOptimizations}\n" +
                                    $"High severity: {highSeverity}\n" +
                                    $"Files optimized: {appliedFiles.Count}")
        {
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(summaryPanel);
    }

    private string GenerateOptimizationReport(List<OptimizationResult> results, Settings settings)
    {
        var report = new StringBuilder();
        report.AppendLine("# Code Optimization Report");
        report.AppendLine();
        report.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Focus:** {settings.Focus}");
        report.AppendLine($"**Files Analyzed:** {results.Count}");
        report.AppendLine();
        
        var totalOptimizations = results.Sum(r => r.Optimizations.Count);
        report.AppendLine($"## Summary");
        report.AppendLine($"- Total optimizations: {totalOptimizations}");
        report.AppendLine($"- High severity: {results.Sum(r => r.Optimizations.Count(o => o.Severity == "high"))}");
        report.AppendLine($"- Medium severity: {results.Sum(r => r.Optimizations.Count(o => o.Severity == "medium"))}");
        report.AppendLine($"- Low severity: {results.Sum(r => r.Optimizations.Count(o => o.Severity == "low"))}");
        report.AppendLine();
        
        report.AppendLine("## Optimizations by File");
        foreach (var result in results.Where(r => r.HasOptimizations))
        {
            report.AppendLine($"### {Path.GetRelativePath(".", result.FilePath)}");
            
            if (!string.IsNullOrEmpty(result.Complexity))
            {
                report.AppendLine($"**Complexity:** {result.Complexity}");
            }
            if (!string.IsNullOrEmpty(result.EstimatedImprovement))
            {
                report.AppendLine($"**Estimated Improvement:** {result.EstimatedImprovement}");
            }
            report.AppendLine();
            
            foreach (var opt in result.Optimizations)
            {
                report.AppendLine($"#### {opt.Description}");
                report.AppendLine($"- **Type:** {opt.Type}");
                report.AppendLine($"- **Severity:** {opt.Severity}");
                report.AppendLine($"- **Location:** {opt.Location}");
                report.AppendLine($"- **Impact:** {opt.Impact}");
                report.AppendLine($"- **Explanation:** {opt.Explanation}");
                
                if (!string.IsNullOrEmpty(opt.Original) && !string.IsNullOrEmpty(opt.Optimized))
                {
                    report.AppendLine();
                    report.AppendLine("**Before:**");
                    report.AppendLine("```");
                    report.AppendLine(opt.Original);
                    report.AppendLine("```");
                    report.AppendLine();
                    report.AppendLine("**After:**");
                    report.AppendLine("```");
                    report.AppendLine(opt.Optimized);
                    report.AppendLine("```");
                }
                report.AppendLine();
            }
        }
        
        return report.ToString();
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

    private class OptimizationResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string OriginalContent { get; set; } = string.Empty;
        public string? OptimizedContent { get; set; }
        public List<Optimization> Optimizations { get; set; } = new();
        public string? Complexity { get; set; }
        public string? EstimatedImprovement { get; set; }
        public bool HasOptimizations => Optimizations.Any() || !string.IsNullOrEmpty(OptimizedContent);
    }

    private class Optimization
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Original { get; set; } = string.Empty;
        public string Optimized { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
}