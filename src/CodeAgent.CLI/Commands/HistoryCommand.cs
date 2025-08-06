using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class HistoryCommand : AsyncCommand<HistoryCommand.Settings>
{
    private readonly IGitService _gitService;
    private readonly IFileSystemService _fileSystemService;

    public HistoryCommand(IGitService gitService, IFileSystemService fileSystemService)
    {
        _gitService = gitService;
        _fileSystemService = fileSystemService;
    }

    public class Settings : CommandSettings
    {
        [Description("Number of commits to show")]
        [CommandOption("-n|--number")]
        public int Count { get; set; } = 10;

        [Description("Show commits for specific file")]
        [CommandOption("-f|--file")]
        public string? File { get; set; }

        [Description("Author to filter by")]
        [CommandOption("-a|--author")]
        public string? Author { get; set; }

        [Description("Show since date (YYYY-MM-DD)")]
        [CommandOption("--since")]
        public string? Since { get; set; }

        [Description("Show until date (YYYY-MM-DD)")]
        [CommandOption("--until")]
        public string? Until { get; set; }

        [Description("Output format (table, tree, graph, simple)")]
        [CommandOption("--format")]
        public OutputFormat Format { get; set; } = OutputFormat.Table;

        [Description("Show detailed diff for each commit")]
        [CommandOption("-d|--diff")]
        public bool ShowDiff { get; set; }

        [Description("Show statistics for each commit")]
        [CommandOption("-s|--stats")]
        public bool ShowStats { get; set; }

        [Description("Search commit messages")]
        [CommandOption("--grep")]
        public string? Grep { get; set; }

        [Description("Export history to file")]
        [CommandOption("-o|--output")]
        public string? OutputFile { get; set; }
    }

    public enum OutputFormat
    {
        Table,
        Tree,
        Graph,
        Simple
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold magenta]Git History[/]"));
            
            // Check if we're in a Git repository
            if (!await _gitService.IsRepositoryAsync("."))
            {
                AnsiConsole.MarkupLine("[red]Error: Not in a Git repository[/]");
                return 1;
            }
            
            // Get commit history
            var commits = await GetFilteredCommits(settings);
            
            if (!commits.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No commits found matching the criteria[/]");
                return 0;
            }
            
            // Display or export history
            if (!string.IsNullOrEmpty(settings.OutputFile))
            {
                await ExportHistory(commits, settings);
            }
            else
            {
                DisplayHistory(commits, settings);
            }
            
            // Show summary
            DisplaySummary(commits);
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<List<GitCommit>> GetFilteredCommits(Settings settings)
    {
        // Get all commits (up to a reasonable limit)
        var allCommits = await _gitService.GetCommitHistoryAsync(".", Math.Max(settings.Count, 100));
        var filteredCommits = allCommits.ToList();
        
        // Filter by author
        if (!string.IsNullOrEmpty(settings.Author))
        {
            filteredCommits = filteredCommits
                .Where(c => c.Author.Contains(settings.Author, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        // Filter by date range
        if (!string.IsNullOrEmpty(settings.Since))
        {
            if (DateTime.TryParse(settings.Since, out var sinceDate))
            {
                filteredCommits = filteredCommits.Where(c => c.Date >= sinceDate).ToList();
            }
        }
        
        if (!string.IsNullOrEmpty(settings.Until))
        {
            if (DateTime.TryParse(settings.Until, out var untilDate))
            {
                filteredCommits = filteredCommits.Where(c => c.Date <= untilDate).ToList();
            }
        }
        
        // Filter by message grep
        if (!string.IsNullOrEmpty(settings.Grep))
        {
            filteredCommits = filteredCommits
                .Where(c => c.Message.Contains(settings.Grep, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        // Filter by file if specified
        if (!string.IsNullOrEmpty(settings.File))
        {
            // Note: This would require extending IGitService to get file-specific history
            AnsiConsole.MarkupLine("[yellow]File-specific history filtering not yet implemented[/]");
        }
        
        // Limit to requested count
        return filteredCommits.Take(settings.Count).ToList();
    }

    private void DisplayHistory(List<GitCommit> commits, Settings settings)
    {
        switch (settings.Format)
        {
            case OutputFormat.Table:
                DisplayAsTable(commits, settings);
                break;
            case OutputFormat.Tree:
                DisplayAsTree(commits, settings);
                break;
            case OutputFormat.Graph:
                DisplayAsGraph(commits, settings);
                break;
            case OutputFormat.Simple:
                DisplayAsSimple(commits, settings);
                break;
        }
    }

    private void DisplayAsTable(List<GitCommit> commits, Settings settings)
    {
        var table = new Table();
        table.AddColumn("Hash");
        table.AddColumn("Date");
        table.AddColumn("Author");
        table.AddColumn("Message");
        
        if (settings.ShowStats)
        {
            table.AddColumn("Files");
        }
        
        table.Border(TableBorder.Rounded);
        
        foreach (var commit in commits)
        {
            var messageLines = commit.Message.Split('\n');
            var shortMessage = messageLines[0].Length > 50 
                ? messageLines[0].Substring(0, 47) + "..." 
                : messageLines[0];
            
            var row = new List<string>
            {
                $"[cyan]{commit.ShortId}[/]",
                commit.Date.ToString("yyyy-MM-dd HH:mm"),
                commit.Author.Length > 20 ? commit.Author.Substring(0, 17) + "..." : commit.Author,
                shortMessage
            };
            
            if (settings.ShowStats)
            {
                row.Add(commit.ModifiedFiles.Count.ToString());
            }
            
            table.AddRow(row.ToArray());
        }
        
        AnsiConsole.Write(table);
        
        // Show diffs if requested
        if (settings.ShowDiff)
        {
            foreach (var commit in commits.Take(3)) // Limit diff display
            {
                AnsiConsole.Write(new Rule($"[cyan]{commit.ShortId}[/]"));
                AnsiConsole.MarkupLine($"[bold]{commit.Message}[/]");
                AnsiConsole.MarkupLine($"[grey]{commit.Author} - {commit.Date:yyyy-MM-dd HH:mm:ss}[/]");
                
                // Note: Would need to extend IGitService to get commit diff
                AnsiConsole.MarkupLine("[yellow]Diff display not yet implemented[/]");
                AnsiConsole.WriteLine();
            }
        }
    }

    private void DisplayAsTree(List<GitCommit> commits, Settings settings)
    {
        var tree = new Tree("[bold]Commit History[/]");
        
        // Group commits by date
        var groupedByDate = commits.GroupBy(c => c.Date.Date)
            .OrderByDescending(g => g.Key);
        
        foreach (var dateGroup in groupedByDate)
        {
            var dateNode = tree.AddNode($"[cyan]{dateGroup.Key:yyyy-MM-dd}[/]");
            
            foreach (var commit in dateGroup.OrderByDescending(c => c.Date))
            {
                var commitInfo = $"[yellow]{commit.ShortId}[/] {commit.Message.Split('\n')[0]}";
                var commitNode = dateNode.AddNode(commitInfo);
                
                if (settings.ShowStats)
                {
                    commitNode.AddNode($"[grey]Author: {commit.Author}[/]");
                    commitNode.AddNode($"[grey]Time: {commit.Date:HH:mm:ss}[/]");
                    
                    if (commit.ModifiedFiles.Any())
                    {
                        commitNode.AddNode($"[grey]Files: {commit.ModifiedFiles.Count}[/]");
                    }
                }
            }
        }
        
        AnsiConsole.Write(tree);
    }

    private void DisplayAsGraph(List<GitCommit> commits, Settings settings)
    {
        // Simple ASCII graph representation
        AnsiConsole.WriteLine();
        
        foreach (var commit in commits)
        {
            // Draw branch line
            AnsiConsole.Markup("[blue]●[/]");
            
            if (commits.IndexOf(commit) < commits.Count - 1)
            {
                AnsiConsole.Markup("[blue]───[/]");
            }
            
            // Commit info
            AnsiConsole.MarkupLine($" [cyan]{commit.ShortId}[/] {commit.Message.Split('\n')[0]}");
            
            if (settings.ShowStats)
            {
                AnsiConsole.MarkupLine($"[blue]│[/]     [grey]{commit.Author}, {commit.Date:yyyy-MM-dd HH:mm}[/]");
            }
            
            if (commits.IndexOf(commit) < commits.Count - 1)
            {
                AnsiConsole.MarkupLine("[blue]│[/]");
            }
        }
        
        AnsiConsole.WriteLine();
    }

    private void DisplayAsSimple(List<GitCommit> commits, Settings settings)
    {
        foreach (var commit in commits)
        {
            AnsiConsole.MarkupLine($"[cyan]{commit.ShortId}[/] {commit.Message}");
            
            if (settings.ShowStats)
            {
                AnsiConsole.MarkupLine($"  Author: {commit.Author}");
                AnsiConsole.MarkupLine($"  Date: {commit.Date:yyyy-MM-dd HH:mm:ss}");
                AnsiConsole.WriteLine();
            }
        }
    }

    private async Task ExportHistory(List<GitCommit> commits, Settings settings)
    {
        var content = new System.Text.StringBuilder();
        
        // Generate content based on file extension
        var extension = Path.GetExtension(settings.OutputFile)?.ToLower();
        
        switch (extension)
        {
            case ".json":
                content.Append(System.Text.Json.JsonSerializer.Serialize(commits, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                }));
                break;
                
            case ".csv":
                content.AppendLine("Hash,Date,Author,Email,Message");
                foreach (var commit in commits)
                {
                    content.AppendLine($"\"{commit.Id}\",\"{commit.Date:yyyy-MM-dd HH:mm:ss}\",\"{commit.Author}\",\"{commit.Email}\",\"{commit.Message.Replace("\"", "\"\"")}\"");
                }
                break;
                
            case ".md":
                content.AppendLine("# Git History");
                content.AppendLine();
                content.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine();
                
                foreach (var commit in commits)
                {
                    content.AppendLine($"## {commit.ShortId}");
                    content.AppendLine($"**Author:** {commit.Author} <{commit.Email}>");
                    content.AppendLine($"**Date:** {commit.Date:yyyy-MM-dd HH:mm:ss}");
                    content.AppendLine();
                    content.AppendLine(commit.Message);
                    content.AppendLine();
                }
                break;
                
            default:
                // Plain text
                foreach (var commit in commits)
                {
                    content.AppendLine($"commit {commit.Id}");
                    content.AppendLine($"Author: {commit.Author} <{commit.Email}>");
                    content.AppendLine($"Date: {commit.Date:yyyy-MM-dd HH:mm:ss}");
                    content.AppendLine();
                    content.AppendLine($"    {commit.Message}");
                    content.AppendLine();
                }
                break;
        }
        
        await _fileSystemService.WriteFileAsync(settings.OutputFile!, content.ToString());
        AnsiConsole.MarkupLine($"[green]✓ History exported to: {settings.OutputFile}[/]");
    }

    private void DisplaySummary(List<GitCommit> commits)
    {
        if (!commits.Any())
            return;
        
        var panel = new Panel(new Rows(
            new Markup($"[bold]Summary[/]"),
            new Markup($"Total commits: {commits.Count}"),
            new Markup($"Date range: {commits.Last().Date:yyyy-MM-dd} to {commits.First().Date:yyyy-MM-dd}"),
            new Markup($"Authors: {commits.Select(c => c.Author).Distinct().Count()}"),
            new Markup($"Most active: {commits.GroupBy(c => c.Author).OrderByDescending(g => g.Count()).First().Key}")
        ))
        {
            Header = new PanelHeader(" History Statistics "),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
    }
}