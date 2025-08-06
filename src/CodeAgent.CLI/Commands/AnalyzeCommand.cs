using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class AnalyzeCommand : AsyncCommand<AnalyzeCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;

    public AnalyzeCommand(IFileSystemService fileSystemService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("Path to the file or directory to analyze")]
        [CommandArgument(0, "<path>")]
        public string Path { get; set; } = ".";

        [Description("Type of analysis to perform")]
        [CommandOption("-t|--type")]
        public AnalysisType Type { get; set; } = AnalysisType.General;

        [Description("Include suggestions for improvements")]
        [CommandOption("-s|--suggestions")]
        public bool IncludeSuggestions { get; set; }

        [Description("Output format (text, json, markdown)")]
        [CommandOption("-f|--format")]
        public OutputFormat Format { get; set; } = OutputFormat.Text;
    }

    public enum AnalysisType
    {
        General,
        Security,
        Performance,
        Architecture,
        CodeQuality,
        Dependencies,
        Complexity
    }

    public enum OutputFormat
    {
        Text,
        Json,
        Markdown
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule($"[bold cyan]Analyzing: {settings.Path}[/]"));
            
            // Determine if path is file or directory
            bool isFile = await _fileSystemService.FileExistsAsync(settings.Path);
            string[] files;
            
            if (isFile)
            {
                files = new[] { settings.Path };
            }
            else
            {
                files = await _fileSystemService.GetFilesAsync(settings.Path, "*", true);
                if (!files.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]No files found in: {settings.Path}[/]");
                    return 0;
                }
            }
            
            AnsiConsole.MarkupLine($"[cyan]Analyzing {files.Length} file(s)...[/]");
            
            // Build analysis context
            var contextBuilder = new System.Text.StringBuilder();
            var fileContents = new Dictionary<string, string>();
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("Reading files...", async ctx =>
                {
                    foreach (var file in files.Take(10)) // Limit to prevent context overflow
                    {
                        var content = await _fileSystemService.ReadFileAsync(file);
                        fileContents[file] = content;
                        contextBuilder.AppendLine($"File: {file}");
                        contextBuilder.AppendLine($"Size: {content.Length} chars");
                        contextBuilder.AppendLine($"Lines: {content.Split('\n').Length}");
                        contextBuilder.AppendLine();
                    }
                });
            
            // Prepare analysis prompt based on type
            var analysisPrompt = BuildAnalysisPrompt(settings.Type, settings.IncludeSuggestions, fileContents, settings.Format);
            
            // Perform analysis
            var analysisResult = new System.Text.StringBuilder();
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync($"Performing {settings.Type} analysis...", async ctx =>
                {
                    await foreach (var chunk in _chatService.StreamResponseAsync(analysisPrompt))
                    {
                        analysisResult.Append(chunk);
                    }
                });
            
            // Display results based on format
            DisplayResults(analysisResult.ToString(), settings.Format);
            
            // If suggestions were included, offer to create tasks
            if (settings.IncludeSuggestions && settings.Format == OutputFormat.Text)
            {
                if (AnsiConsole.Confirm("Would you like to see specific improvement tasks?"))
                {
                    await GenerateImprovementTasks(analysisResult.ToString());
                }
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private string BuildAnalysisPrompt(AnalysisType type, bool includeSuggestions, Dictionary<string, string> fileContents, OutputFormat format)
    {
        var prompt = new System.Text.StringBuilder();
        prompt.AppendLine($"Perform a {type} analysis of the following code:");
        prompt.AppendLine();
        
        foreach (var (file, content) in fileContents)
        {
            prompt.AppendLine($"=== File: {file} ===");
            prompt.AppendLine(content.Length > 5000 ? content.Substring(0, 5000) + "..." : content);
            prompt.AppendLine();
        }
        
        prompt.AppendLine("\nAnalysis requirements:");
        
        switch (type)
        {
            case AnalysisType.Security:
                prompt.AppendLine("- Identify security vulnerabilities");
                prompt.AppendLine("- Check for unsafe practices");
                prompt.AppendLine("- Review authentication and authorization");
                prompt.AppendLine("- Examine data validation and sanitization");
                break;
            case AnalysisType.Performance:
                prompt.AppendLine("- Identify performance bottlenecks");
                prompt.AppendLine("- Review algorithm efficiency");
                prompt.AppendLine("- Check for memory leaks or inefficient memory usage");
                prompt.AppendLine("- Examine database queries and I/O operations");
                break;
            case AnalysisType.Architecture:
                prompt.AppendLine("- Review architectural patterns");
                prompt.AppendLine("- Evaluate separation of concerns");
                prompt.AppendLine("- Check dependency management");
                prompt.AppendLine("- Assess modularity and reusability");
                break;
            case AnalysisType.CodeQuality:
                prompt.AppendLine("- Review code readability and maintainability");
                prompt.AppendLine("- Check naming conventions");
                prompt.AppendLine("- Evaluate error handling");
                prompt.AppendLine("- Assess test coverage needs");
                break;
            case AnalysisType.Dependencies:
                prompt.AppendLine("- List all external dependencies");
                prompt.AppendLine("- Check for outdated packages");
                prompt.AppendLine("- Identify security vulnerabilities in dependencies");
                prompt.AppendLine("- Review license compatibility");
                break;
            case AnalysisType.Complexity:
                prompt.AppendLine("- Calculate cyclomatic complexity");
                prompt.AppendLine("- Identify overly complex methods");
                prompt.AppendLine("- Review coupling and cohesion");
                prompt.AppendLine("- Suggest simplification opportunities");
                break;
            default:
                prompt.AppendLine("- Provide a comprehensive code review");
                prompt.AppendLine("- Identify potential issues");
                prompt.AppendLine("- Review best practices adherence");
                break;
        }
        
        if (includeSuggestions)
        {
            prompt.AppendLine("\nInclude specific, actionable suggestions for improvement.");
        }
        
        switch (format)
        {
            case OutputFormat.Json:
                prompt.AppendLine("\nFormat the response as valid JSON with the following structure:");
                prompt.AppendLine("{ \"summary\": \"\", \"findings\": [], \"suggestions\": [] }");
                break;
            case OutputFormat.Markdown:
                prompt.AppendLine("\nFormat the response as Markdown with appropriate headers and lists.");
                break;
            default:
                prompt.AppendLine("\nProvide a clear, structured text response.");
                break;
        }
        
        return prompt.ToString();
    }

    private void DisplayResults(string results, OutputFormat format)
    {
        switch (format)
        {
            case OutputFormat.Json:
                AnsiConsole.WriteLine(results);
                break;
            case OutputFormat.Markdown:
                // Simple markdown rendering
                var lines = results.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("# "))
                        AnsiConsole.MarkupLine($"[bold cyan]{line.Substring(2)}[/]");
                    else if (line.StartsWith("## "))
                        AnsiConsole.MarkupLine($"[bold]{line.Substring(3)}[/]");
                    else if (line.StartsWith("- "))
                        AnsiConsole.MarkupLine($"  • {line.Substring(2)}");
                    else
                        AnsiConsole.WriteLine(line);
                }
                break;
            default:
                var panel = new Panel(results)
                {
                    Header = new PanelHeader(" Analysis Results "),
                    Border = BoxBorder.Rounded
                };
                AnsiConsole.Write(panel);
                break;
        }
    }

    private async Task GenerateImprovementTasks(string analysisResult)
    {
        var taskPrompt = $"Based on this analysis:\n{analysisResult}\n\n" +
                        "Generate a numbered list of specific, actionable improvement tasks. " +
                        "Each task should be concrete and implementable. Format as:\n" +
                        "1. Task description\n2. Task description\netc.";
        
        var tasks = new System.Text.StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(taskPrompt))
        {
            tasks.Append(chunk);
        }
        
        AnsiConsole.Write(new Rule("[bold]Improvement Tasks[/]"));
        AnsiConsole.WriteLine(tasks.ToString());
    }
}