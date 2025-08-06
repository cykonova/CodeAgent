using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class ApplyCommand : AsyncCommand<ApplyCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;

    public ApplyCommand(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    public class Settings : CommandSettings
    {
        [Description("Path to the file to apply changes for")]
        [CommandArgument(0, "[file]")]
        public string? FilePath { get; set; }

        [Description("Apply all pending changes without confirmation")]
        [CommandOption("--all")]
        public bool ApplyAll { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var pendingOperations = await _fileSystemService.GetPendingOperationsAsync();
            
            if (!pendingOperations.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No pending changes to apply.[/]");
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

            // Confirm before applying
            if (!settings.ApplyAll)
            {
                var operationCount = pendingOperations.Count();
                var message = operationCount == 1 
                    ? "Apply 1 pending change?" 
                    : $"Apply {operationCount} pending changes?";
                    
                if (!AnsiConsole.Confirm(message))
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                    return 0;
                }
            }

            // Apply changes
            var appliedCount = 0;
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Applying changes...[/]", maxValue: pendingOperations.Count());
                    
                    foreach (var operation in pendingOperations)
                    {
                        await _fileSystemService.ApplyOperationAsync(operation);
                        appliedCount++;
                        task.Increment(1);
                        task.Description = $"[green]Applied: {operation.FilePath}[/]";
                    }
                });

            AnsiConsole.MarkupLine($"[green]✓ Successfully applied {appliedCount} change(s)[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}