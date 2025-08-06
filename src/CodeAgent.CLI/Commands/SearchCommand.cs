using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.RegularExpressions;

namespace CodeAgent.CLI.Commands;

public class SearchCommand : AsyncCommand<SearchCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;

    public SearchCommand(IFileSystemService fileSystemService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("Search pattern or query")]
        [CommandArgument(0, "<pattern>")]
        public string Pattern { get; set; } = string.Empty;

        [Description("Directory to search in")]
        [CommandOption("-d|--directory")]
        public string Directory { get; set; } = ".";

        [Description("File pattern to search")]
        [CommandOption("-f|--file-pattern")]
        public string FilePattern { get; set; } = "*";

        [Description("Use AI to understand context")]
        [CommandOption("--ai")]
        public bool UseAI { get; set; }

        [Description("Case insensitive search")]
        [CommandOption("-i|--ignore-case")]
        public bool IgnoreCase { get; set; }

        [Description("Use regex pattern")]
        [CommandOption("-r|--regex")]
        public bool UseRegex { get; set; }

        [Description("Show context lines around matches")]
        [CommandOption("-c|--context")]
        public int ContextLines { get; set; } = 2;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule($"[bold cyan]Searching for: {settings.Pattern}[/]"));
            
            // Get files to search
            var files = await _fileSystemService.GetFilesAsync(settings.Directory, settings.FilePattern, true);
            
            if (!files.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No files found matching pattern: {settings.FilePattern}[/]");
                return 0;
            }
            
            var matches = new List<SearchMatch>();
            var searchOptions = settings.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            var searchPattern = settings.UseRegex ? settings.Pattern : Regex.Escape(settings.Pattern);
            var regex = new Regex(searchPattern, searchOptions);
            
            // Search through files
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Searching files...[/]", maxValue: files.Length);
                    
                    foreach (var file in files)
                    {
                        var content = await _fileSystemService.ReadFileAsync(file);
                        var lines = content.Split('\n');
                        
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (regex.IsMatch(lines[i]))
                            {
                                var match = new SearchMatch
                                {
                                    FilePath = file,
                                    LineNumber = i + 1,
                                    Line = lines[i],
                                    ContextBefore = GetContextLines(lines, i, settings.ContextLines, true),
                                    ContextAfter = GetContextLines(lines, i, settings.ContextLines, false)
                                };
                                matches.Add(match);
                            }
                        }
                        
                        task.Increment(1);
                    }
                });
            
            if (!matches.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No matches found for pattern: {settings.Pattern}[/]");
                return 0;
            }
            
            // Display results
            AnsiConsole.MarkupLine($"[green]Found {matches.Count} matches in {matches.Select(m => m.FilePath).Distinct().Count()} files[/]");
            AnsiConsole.WriteLine();
            
            // Group matches by file
            var groupedMatches = matches.GroupBy(m => m.FilePath);
            
            foreach (var group in groupedMatches)
            {
                AnsiConsole.Write(new Rule($"[bold]{group.Key}[/]"));
                
                foreach (var match in group)
                {
                    // Display context before
                    foreach (var contextLine in match.ContextBefore)
                    {
                        AnsiConsole.MarkupLine($"[dim]  {contextLine}[/]");
                    }
                    
                    // Display matching line with highlighting
                    var highlightedLine = regex.Replace(match.Line, m => $"[yellow on red]{m.Value}[/]");
                    AnsiConsole.MarkupLine($"[cyan]{match.LineNumber,4}:[/] {Markup.Escape(match.Line.Substring(0, regex.Match(match.Line).Index))}{highlightedLine.Substring(regex.Match(match.Line).Index)}");
                    
                    // Display context after
                    foreach (var contextLine in match.ContextAfter)
                    {
                        AnsiConsole.MarkupLine($"[dim]  {contextLine}[/]");
                    }
                    
                    AnsiConsole.WriteLine();
                }
            }
            
            // AI-powered context understanding
            if (settings.UseAI && matches.Any())
            {
                AnsiConsole.Write(new Rule("[bold]AI Analysis[/]"));
                
                var contextPrompt = $"Analyze these search results for pattern '{settings.Pattern}':\n\n";
                foreach (var match in matches.Take(10)) // Limit for context
                {
                    contextPrompt += $"File: {match.FilePath}, Line {match.LineNumber}: {match.Line}\n";
                }
                contextPrompt += "\nProvide insights about:\n";
                contextPrompt += "1. What these matches represent\n";
                contextPrompt += "2. Common patterns or usage\n";
                contextPrompt += "3. Potential refactoring opportunities\n";
                contextPrompt += "Be concise and specific.";
                
                var analysis = new System.Text.StringBuilder();
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync("Analyzing search results...", async ctx =>
                    {
                        await foreach (var chunk in _chatService.StreamResponseAsync(contextPrompt))
                        {
                            analysis.Append(chunk);
                        }
                    });
                
                var panel = new Panel(analysis.ToString())
                {
                    Header = new PanelHeader(" AI Insights "),
                    Border = BoxBorder.Rounded
                };
                AnsiConsole.Write(panel);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private List<string> GetContextLines(string[] lines, int currentIndex, int contextCount, bool before)
    {
        var result = new List<string>();
        
        if (before)
        {
            var start = Math.Max(0, currentIndex - contextCount);
            for (int i = start; i < currentIndex; i++)
            {
                result.Add(lines[i]);
            }
        }
        else
        {
            var end = Math.Min(lines.Length - 1, currentIndex + contextCount);
            for (int i = currentIndex + 1; i <= end; i++)
            {
                result.Add(lines[i]);
            }
        }
        
        return result;
    }

    private class SearchMatch
    {
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Line { get; set; } = string.Empty;
        public List<string> ContextBefore { get; set; } = new();
        public List<string> ContextAfter { get; set; } = new();
    }
}