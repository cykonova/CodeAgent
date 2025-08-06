using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class DiffCommand : AsyncCommand<DiffCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IDiffService _diffService;

    public DiffCommand(IFileSystemService fileSystemService, IDiffService diffService)
    {
        _fileSystemService = fileSystemService;
        _diffService = diffService;
    }

    public class Settings : CommandSettings
    {
        [Description("Path to the file to show diff for")]
        [CommandArgument(0, "[file]")]
        public string? FilePath { get; set; }

        [Description("Show unified diff format")]
        [CommandOption("--unified")]
        public bool UnifiedDiff { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var pendingOperations = await _fileSystemService.GetPendingOperationsAsync();
            
            if (!pendingOperations.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No pending changes to display.[/]");
                return 0;
            }

            // Filter by file path if specified
            if (!string.IsNullOrEmpty(settings.FilePath))
            {
                pendingOperations = pendingOperations.Where(o => o.FilePath == settings.FilePath);
                if (!pendingOperations.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]No pending changes for file: {settings.FilePath}[/]");
                    return 0;
                }
            }

            // Display diff for each pending operation
            foreach (var operation in pendingOperations)
            {
                AnsiConsole.Write(new Rule($"[bold]{operation.FilePath}[/]"));
                
                if (operation.Type == FileOperationType.Write || operation.Type == FileOperationType.Create)
                {
                    var originalContent = operation.OriginalContent ?? string.Empty;
                    var newContent = operation.NewContent ?? string.Empty;
                    
                    var diffResult = await _diffService.GenerateDiffAsync(originalContent, newContent, operation.FilePath);
                    
                    if (settings.UnifiedDiff)
                    {
                        AnsiConsole.WriteLine(diffResult.UnifiedDiff);
                    }
                    else
                    {
                        DisplayColoredDiff(diffResult);
                    }
                    
                    // Show summary
                    AnsiConsole.MarkupLine($"[green]Added:[/] {diffResult.AddedLines} | [red]Deleted:[/] {diffResult.DeletedLines} | [yellow]Modified:[/] {diffResult.ModifiedLines}");
                }
                else if (operation.Type == FileOperationType.Delete)
                {
                    AnsiConsole.MarkupLine($"[red]File will be deleted[/]");
                }
                
                AnsiConsole.WriteLine();
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private void DisplayColoredDiff(DiffResult diffResult)
    {
        foreach (var line in diffResult.Lines)
        {
            switch (line.Type)
            {
                case DiffLineType.Added:
                    AnsiConsole.MarkupLine($"[green]+ {Markup.Escape(line.Content)}[/]");
                    break;
                case DiffLineType.Deleted:
                    AnsiConsole.MarkupLine($"[red]- {Markup.Escape(line.Content)}[/]");
                    break;
                case DiffLineType.Unchanged:
                    if (diffResult.Lines.Count < 50) // Only show unchanged lines for small diffs
                    {
                        AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(line.Content)}[/]");
                    }
                    break;
            }
        }
    }
}