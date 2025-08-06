using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class EditCommand : AsyncCommand<EditCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IDiffService _diffService;
    private readonly IChatService _chatService;

    public EditCommand(IFileSystemService fileSystemService, IDiffService diffService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _diffService = diffService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("Path to the file to edit")]
        [CommandArgument(0, "<file>")]
        public string FilePath { get; set; } = string.Empty;

        [Description("Description of the changes to make")]
        [CommandArgument(1, "<description>")]
        public string Description { get; set; } = string.Empty;

        [Description("Auto-approve changes without preview")]
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
            // Validate file exists
            if (!await _fileSystemService.FileExistsAsync(settings.FilePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: File not found: {settings.FilePath}[/]");
                return 1;
            }

            // Read original content
            var originalContent = await _fileSystemService.ReadFileAsync(settings.FilePath);
            
            // Show status
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync($"Analyzing file and generating changes...", async ctx =>
                {
                    await Task.Delay(500); // Simulate processing
                });

            // Request AI to modify the file
            var prompt = $"Modify the following file according to this description: {settings.Description}\n\n" +
                        $"File: {settings.FilePath}\n" +
                        $"Content:\n{originalContent}\n\n" +
                        "Return only the modified file content, no explanations.";

            string modifiedContent = originalContent; // Default to original if AI fails
            
            // Stream the response and collect it
            var responseBuilder = new System.Text.StringBuilder();
            await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
            {
                responseBuilder.Append(chunk);
            }
            modifiedContent = CleanMarkdownFences(responseBuilder.ToString());

            // Generate diff
            var diffResult = await _diffService.GenerateDiffAsync(originalContent, modifiedContent, settings.FilePath);

            if (!diffResult.HasChanges)
            {
                AnsiConsole.MarkupLine("[yellow]No changes detected.[/]");
                return 0;
            }

            // Display diff
            DisplayDiff(diffResult, settings.UnifiedDiff);

            // Show summary
            var panel = new Panel(
                $"[green]Added:[/] {diffResult.AddedLines} lines\n" +
                $"[red]Deleted:[/] {diffResult.DeletedLines} lines\n" +
                $"[yellow]Modified:[/] {diffResult.ModifiedLines} lines")
            {
                Header = new PanelHeader(" Changes Summary "),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);

            // Ask for confirmation unless auto-approve
            bool shouldApply = settings.AutoApprove;
            if (!settings.AutoApprove)
            {
                shouldApply = AnsiConsole.Confirm("Apply these changes?");
            }

            if (shouldApply)
            {
                // Create preview operation
                var operation = await _fileSystemService.PreviewWriteAsync(settings.FilePath, modifiedContent);
                
                // Apply the operation
                await _fileSystemService.ApplyOperationAsync(operation);
                
                AnsiConsole.MarkupLine($"[green]✓ Changes applied successfully to {settings.FilePath}[/]");
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Changes rejected.[/]");
                return 0;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private string CleanMarkdownFences(string content)
    {
        // Remove markdown code fences that LLMs sometimes add
        var lines = content.Split('\n').ToList();
        var cleanedLines = new List<string>();
        bool inCodeBlock = false;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Check for opening fence with optional language identifier
            if (trimmedLine.StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    inCodeBlock = true;
                    // Skip the opening fence line
                    continue;
                }
                else
                {
                    inCodeBlock = false;
                    // Skip the closing fence line
                    continue;
                }
            }
            
            cleanedLines.Add(line);
        }
        
        return string.Join('\n', cleanedLines);
    }
    
    private void DisplayDiff(DiffResult diffResult, bool unifiedFormat)
    {
        if (unifiedFormat)
        {
            AnsiConsole.WriteLine(diffResult.UnifiedDiff);
        }
        else
        {
            // Display side-by-side or line-by-line diff
            var table = new Table();
            table.AddColumn("Line");
            table.AddColumn("Original");
            table.AddColumn("Modified");
            table.Border(TableBorder.Rounded);

            foreach (var line in diffResult.Lines)
            {
                switch (line.Type)
                {
                    case DiffLineType.Unchanged:
                        table.AddRow(
                            line.OriginalLineNumber?.ToString() ?? "",
                            $"[dim]{Markup.Escape(line.Content)}[/]",
                            $"[dim]{Markup.Escape(line.Content)}[/]"
                        );
                        break;
                    case DiffLineType.Added:
                        table.AddRow(
                            line.ModifiedLineNumber?.ToString() ?? "",
                            "",
                            $"[green]+ {Markup.Escape(line.Content)}[/]"
                        );
                        break;
                    case DiffLineType.Deleted:
                        table.AddRow(
                            line.OriginalLineNumber?.ToString() ?? "",
                            $"[red]- {Markup.Escape(line.Content)}[/]",
                            ""
                        );
                        break;
                }
            }

            AnsiConsole.Write(table);
        }
    }
}