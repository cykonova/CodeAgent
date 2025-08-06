using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class RefactorCommand : AsyncCommand<RefactorCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IDiffService _diffService;
    private readonly IChatService _chatService;

    public RefactorCommand(IFileSystemService fileSystemService, IDiffService diffService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _diffService = diffService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("Description of the refactoring to perform")]
        [CommandArgument(0, "<description>")]
        public string Description { get; set; } = string.Empty;

        [Description("Root directory to refactor (default: current)")]
        [CommandOption("-d|--directory")]
        public string Directory { get; set; } = ".";

        [Description("File pattern to include (e.g., '*.cs')")]
        [CommandOption("-p|--pattern")]
        public string Pattern { get; set; } = "*";

        [Description("Auto-approve all changes")]
        [CommandOption("--auto-approve")]
        public bool AutoApprove { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule($"[bold cyan]Refactoring: {settings.Description}[/]"));
            
            // Get all relevant files
            var files = await _fileSystemService.GetFilesAsync(settings.Directory, settings.Pattern, true);
            
            if (!files.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No files found matching pattern: {settings.Pattern}[/]");
                return 0;
            }

            // First, analyze the codebase to understand the refactoring scope
            AnsiConsole.MarkupLine($"[cyan]Analyzing {files.Length} files for refactoring opportunities...[/]");
            
            // Build context from all files
            var codebaseContext = new System.Text.StringBuilder();
            codebaseContext.AppendLine("Codebase structure:");
            foreach (var file in files.Take(20)) // Limit context size
            {
                codebaseContext.AppendLine($"- {file}");
            }
            
            // Ask AI to plan the refactoring
            var planPrompt = $"Plan a refactoring for the following request:\n" +
                           $"Request: {settings.Description}\n\n" +
                           $"Codebase files:\n{codebaseContext}\n\n" +
                           "Provide a structured plan listing:\n" +
                           "1. Files that need to be modified\n" +
                           "2. Files that might need to be created\n" +
                           "3. Key changes for each file\n" +
                           "Be specific and concise.";
            
            var planResponse = new System.Text.StringBuilder();
            await foreach (var chunk in _chatService.StreamResponseAsync(planPrompt))
            {
                planResponse.Append(chunk);
            }
            
            // Display the refactoring plan
            var panel = new Panel(planResponse.ToString())
            {
                Header = new PanelHeader(" Refactoring Plan "),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);
            
            if (!settings.AutoApprove && !AnsiConsole.Confirm("Proceed with this refactoring plan?"))
            {
                AnsiConsole.MarkupLine("[yellow]Refactoring cancelled.[/]");
                return 0;
            }
            
            // Execute the refactoring
            var pendingOperations = new List<FileOperation>();
            var affectedFiles = new List<string>();
            
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Refactoring files...[/]", maxValue: files.Length);
                    
                    foreach (var file in files)
                    {
                        var originalContent = await _fileSystemService.ReadFileAsync(file);
                        
                        // Ask AI to refactor this specific file
                        var refactorPrompt = $"Refactor the following file according to this plan:\n" +
                                           $"Overall refactoring: {settings.Description}\n" +
                                           $"Refactoring plan:\n{planResponse}\n\n" +
                                           $"File: {file}\n" +
                                           $"Content:\n{originalContent}\n\n" +
                                           "If this file doesn't need changes, return EXACTLY the original content unchanged.\n" +
                                           "Otherwise, return the refactored content. No explanations.";
                        
                        var responseBuilder = new System.Text.StringBuilder();
                        await foreach (var chunk in _chatService.StreamResponseAsync(refactorPrompt))
                        {
                            responseBuilder.Append(chunk);
                        }
                        var modifiedContent = responseBuilder.ToString();
                        
                        // Check if content actually changed
                        if (modifiedContent.Trim() != originalContent.Trim())
                        {
                            var operation = await _fileSystemService.PreviewWriteAsync(file, modifiedContent);
                            pendingOperations.Add(operation);
                            affectedFiles.Add(file);
                        }
                        
                        task.Increment(1);
                    }
                });
            
            if (!pendingOperations.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No files required changes.[/]");
                return 0;
            }
            
            // Show summary of changes
            AnsiConsole.Write(new Rule("[bold]Refactoring Summary[/]"));
            AnsiConsole.MarkupLine($"[green]Files to be modified: {pendingOperations.Count}[/]");
            
            foreach (var file in affectedFiles)
            {
                AnsiConsole.MarkupLine($"  • {file}");
            }
            
            // Final confirmation
            bool shouldApply = settings.AutoApprove;
            if (!settings.AutoApprove)
            {
                shouldApply = AnsiConsole.Confirm($"Apply refactoring to {pendingOperations.Count} files?");
            }
            
            if (shouldApply)
            {
                foreach (var operation in pendingOperations)
                {
                    await _fileSystemService.ApplyOperationAsync(operation);
                }
                AnsiConsole.MarkupLine($"[green]✓ Refactoring completed successfully![/]");
            }
            else
            {
                await _fileSystemService.ClearPendingOperationsAsync();
                AnsiConsole.MarkupLine("[yellow]Refactoring cancelled.[/]");
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