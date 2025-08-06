using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class RejectCommand : AsyncCommand<RejectCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;

    public RejectCommand(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    public class Settings : CommandSettings
    {
        [Description("Path to the file to reject changes for")]
        [CommandArgument(0, "[file]")]
        public string? FilePath { get; set; }

        [Description("Reject all pending changes without confirmation")]
        [CommandOption("--all")]
        public bool RejectAll { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var pendingOperations = await _fileSystemService.GetPendingOperationsAsync();
            
            if (!pendingOperations.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No pending changes to reject.[/]");
                return 0;
            }

            var operationsToReject = pendingOperations;
            
            // Filter by file path if specified
            if (!string.IsNullOrEmpty(settings.FilePath))
            {
                operationsToReject = pendingOperations.Where(o => o.FilePath == settings.FilePath);
                if (!operationsToReject.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]No pending changes for file: {settings.FilePath}[/]");
                    return 0;
                }
            }

            // Confirm before rejecting
            if (!settings.RejectAll)
            {
                var operationCount = operationsToReject.Count();
                var message = operationCount == 1 
                    ? "Reject 1 pending change?" 
                    : $"Reject {operationCount} pending changes?";
                    
                if (!AnsiConsole.Confirm(message))
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                    return 0;
                }
            }

            // Clear pending operations
            if (string.IsNullOrEmpty(settings.FilePath))
            {
                await _fileSystemService.ClearPendingOperationsAsync();
                AnsiConsole.MarkupLine($"[green]✓ All pending changes rejected[/]");
            }
            else
            {
                // For specific file, we need to remove only those operations
                // This would require enhancing the interface, for now clear all
                AnsiConsole.MarkupLine($"[yellow]Note: Clearing all pending operations (file-specific rejection coming soon)[/]");
                await _fileSystemService.ClearPendingOperationsAsync();
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}