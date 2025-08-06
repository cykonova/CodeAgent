using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class EditMultipleCommand : AsyncCommand<EditMultipleCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IDiffService _diffService;
    private readonly IChatService _chatService;

    public EditMultipleCommand(IFileSystemService fileSystemService, IDiffService diffService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _diffService = diffService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("Description of the changes to make across multiple files")]
        [CommandArgument(0, "<description>")]
        public string Description { get; set; } = string.Empty;

        [Description("Pattern to match files (e.g., '*.cs', 'src/**/*.ts')")]
        [CommandOption("-p|--pattern")]
        public string? Pattern { get; set; }

        [Description("Auto-approve all changes without preview")]
        [CommandOption("--auto-approve")]
        public bool AutoApprove { get; set; }

        [Description("Show unified diff format")]
        [CommandOption("--unified")]
        public bool UnifiedDiff { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Get files to edit
            var pattern = settings.Pattern ?? "*.cs";
            var files = await _fileSystemService.GetFilesAsync(".", pattern, true);
            
            if (!files.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No files found matching pattern: {pattern}[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[cyan]Found {files.Length} files matching pattern: {pattern}[/]");
            
            // Analyze files and determine which ones need changes
            var filesToEdit = new List<(string path, string originalContent)>();
            
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing files...[/]", maxValue: files.Length);
                    
                    foreach (var file in files)
                    {
                        var content = await _fileSystemService.ReadFileAsync(file);
                        
                        // Ask AI if this file needs changes based on the description
                        var analysisPrompt = $"Does this file need changes based on the following description?\n" +
                                           $"Description: {settings.Description}\n" +
                                           $"File: {file}\n" +
                                           $"Content preview (first 500 chars):\n{content.Substring(0, Math.Min(500, content.Length))}\n\n" +
                                           "Reply with only 'YES' or 'NO'.";
                        
                        var response = new System.Text.StringBuilder();
                        await foreach (var chunk in _chatService.StreamResponseAsync(analysisPrompt))
                        {
                            response.Append(chunk);
                        }
                        
                        if (response.ToString().Trim().StartsWith("YES", StringComparison.OrdinalIgnoreCase))
                        {
                            filesToEdit.Add((file, content));
                        }
                        
                        task.Increment(1);
                    }
                });

            if (!filesToEdit.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No files require changes based on the description.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[green]{filesToEdit.Count} files will be modified[/]");
            
            // Process each file
            var pendingOperations = new List<FileOperation>();
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync($"Generating changes for {filesToEdit.Count} files...", async ctx =>
                {
                    foreach (var (filePath, originalContent) in filesToEdit)
                    {
                        ctx.Status($"Processing {filePath}...");
                        
                        // Request AI to modify the file
                        var prompt = $@"Task: Modify the file '{filePath}' according to the following requirements:
{settings.Description}

Current file content:
{originalContent}

Instructions:
- Apply the requested changes to the file content above
- Return the COMPLETE modified file content
- Do NOT include any explanations, comments about changes, or markdown formatting
- The output will be written directly to the file, so it must be valid code";

                        var responseBuilder = new System.Text.StringBuilder();
                        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
                        {
                            responseBuilder.Append(chunk);
                        }
                        var modifiedContent = responseBuilder.ToString();

                        // Create preview operation if content changed
                        if (modifiedContent != originalContent)
                        {
                            var operation = await _fileSystemService.PreviewWriteAsync(filePath, modifiedContent);
                            pendingOperations.Add(operation);
                        }
                    }
                });

            if (!pendingOperations.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No changes were generated.[/]");
                return 0;
            }

            // Display summary of changes
            AnsiConsole.Write(new Rule("[bold]Changes Summary[/]"));
            var table = new Table();
            table.AddColumn("File");
            table.AddColumn("Status");
            table.Border(TableBorder.Rounded);

            foreach (var op in pendingOperations)
            {
                var diffResult = await _diffService.GenerateDiffAsync(
                    op.OriginalContent ?? string.Empty,
                    op.NewContent ?? string.Empty,
                    op.FilePath);
                
                table.AddRow(
                    op.FilePath,
                    $"[green]+{diffResult.AddedLines}[/] [red]-{diffResult.DeletedLines}[/]"
                );
            }
            AnsiConsole.Write(table);

            // Ask for confirmation unless auto-approve
            bool shouldApply = settings.AutoApprove;
            if (!settings.AutoApprove)
            {
                shouldApply = AnsiConsole.Confirm($"Apply changes to {pendingOperations.Count} files?");
            }

            if (shouldApply)
            {
                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Applying changes...[/]", maxValue: pendingOperations.Count);
                        
                        foreach (var operation in pendingOperations)
                        {
                            await _fileSystemService.ApplyOperationAsync(operation);
                            task.Increment(1);
                        }
                    });
                
                AnsiConsole.MarkupLine($"[green]✓ Successfully modified {pendingOperations.Count} files[/]");
            }
            else
            {
                await _fileSystemService.ClearPendingOperationsAsync();
                AnsiConsole.MarkupLine("[yellow]Changes rejected.[/]");
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