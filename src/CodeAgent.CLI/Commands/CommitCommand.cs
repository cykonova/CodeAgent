using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class CommitCommand : AsyncCommand<CommitCommand.Settings>
{
    private readonly IGitService _gitService;
    private readonly IChatService _chatService;
    private readonly IFileSystemService _fileSystemService;

    public CommitCommand(IGitService gitService, IChatService chatService, IFileSystemService fileSystemService)
    {
        _gitService = gitService;
        _chatService = chatService;
        _fileSystemService = fileSystemService;
    }

    public class Settings : CommandSettings
    {
        [Description("Commit message (auto-generated if not provided)")]
        [CommandArgument(0, "[message]")]
        public string? Message { get; set; }

        [Description("Stage all changes before committing")]
        [CommandOption("-a|--all")]
        public bool StageAll { get; set; }

        [Description("Commit type (feat, fix, docs, style, refactor, test, chore)")]
        [CommandOption("-t|--type")]
        public string? Type { get; set; }

        [Description("Scope of the commit")]
        [CommandOption("-s|--scope")]
        public string? Scope { get; set; }

        [Description("Use conventional commits format")]
        [CommandOption("--conventional")]
        public bool UseConventionalCommits { get; set; } = true;

        [Description("Include breaking change note")]
        [CommandOption("--breaking")]
        public bool BreakingChange { get; set; }

        [Description("Interactive mode for message editing")]
        [CommandOption("-i|--interactive")]
        public bool Interactive { get; set; }

        [Description("Amend the last commit")]
        [CommandOption("--amend")]
        public bool Amend { get; set; }

        [Description("Sign the commit")]
        [CommandOption("--sign")]
        public bool Sign { get; set; }

        [Description("Dry run - show what would be committed")]
        [CommandOption("--dry-run")]
        public bool DryRun { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold green]Git Commit[/]"));
            
            // Check if we're in a Git repository
            if (!await _gitService.IsRepositoryAsync("."))
            {
                AnsiConsole.MarkupLine("[red]Error: Not in a Git repository[/]");
                return 1;
            }
            
            // Stage changes if requested
            if (settings.StageAll)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync("Staging all changes...", async ctx =>
                    {
                        await _gitService.StageAllAsync(".");
                    });
            }
            
            // Get current status
            var modifiedFiles = await _gitService.GetModifiedFilesAsync(".");
            var untrackedFiles = await _gitService.GetUntrackedFilesAsync(".");
            var hasChanges = await _gitService.HasUncommittedChangesAsync(".");
            
            if (!hasChanges && !settings.Amend)
            {
                AnsiConsole.MarkupLine("[yellow]No changes to commit[/]");
                return 0;
            }
            
            // Display changes to be committed
            await DisplayChangesToCommit(modifiedFiles, untrackedFiles);
            
            // Generate or get commit message
            string commitMessage;
            if (!string.IsNullOrEmpty(settings.Message))
            {
                commitMessage = settings.Message;
            }
            else
            {
                commitMessage = await GenerateCommitMessage(settings);
                
                if (settings.Interactive)
                {
                    commitMessage = await EditCommitMessage(commitMessage);
                }
            }
            
            // Format message if using conventional commits
            if (settings.UseConventionalCommits)
            {
                commitMessage = FormatConventionalCommit(commitMessage, settings);
            }
            
            // Display what will be committed
            DisplayCommitPreview(commitMessage, modifiedFiles.Count() + untrackedFiles.Count());
            
            // Confirm if not dry run
            if (!settings.DryRun)
            {
                var confirm = AnsiConsole.Confirm("Proceed with commit?");
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Commit cancelled[/]");
                    return 0;
                }
                
                // Perform the commit
                var success = await _gitService.CommitAsync(".", commitMessage);
                
                if (success)
                {
                    AnsiConsole.MarkupLine("[green]✓ Changes committed successfully![/]");
                    
                    // Show commit details
                    var commits = await _gitService.GetCommitHistoryAsync(".", 1);
                    var latestCommit = commits.FirstOrDefault();
                    if (latestCommit != null)
                    {
                        AnsiConsole.MarkupLine($"[cyan]Commit: {latestCommit.ShortId}[/]");
                        AnsiConsole.MarkupLine($"[cyan]Author: {latestCommit.Author} <{latestCommit.Email}>[/]");
                        AnsiConsole.MarkupLine($"[cyan]Date: {latestCommit.Date:yyyy-MM-dd HH:mm:ss}[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗ Commit failed[/]");
                    return 1;
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Dry run - no commit performed[/]");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private Task DisplayChangesToCommit(IEnumerable<string> modifiedFiles, IEnumerable<string> untrackedFiles)
    {
        var table = new Table();
        table.AddColumn("Status");
        table.AddColumn("File");
        table.Border(TableBorder.Rounded);
        table.Title = new TableTitle("Changes to be committed");
        
        foreach (var file in modifiedFiles)
        {
            table.AddRow("[yellow]Modified[/]", file);
        }
        
        foreach (var file in untrackedFiles)
        {
            table.AddRow("[green]New[/]", file);
        }
        
        if (!modifiedFiles.Any() && !untrackedFiles.Any())
        {
            table.AddRow("[grey]No changes[/]", "-");
        }
        
        AnsiConsole.Write(table);
        return Task.CompletedTask;
    }

    private async Task<string> GenerateCommitMessage(Settings settings)
    {
        AnsiConsole.MarkupLine("[cyan]Generating commit message using AI...[/]");
        
        // Get diff for context
        var diff = await _gitService.GetDiffAsync(".");
        
        // Build prompt
        var prompt = new StringBuilder();
        prompt.AppendLine("Generate a commit message for the following changes:");
        prompt.AppendLine();
        prompt.AppendLine("Diff:");
        prompt.AppendLine(diff.Length > 5000 ? diff.Substring(0, 5000) + "..." : diff);
        prompt.AppendLine();
        prompt.AppendLine("Requirements:");
        prompt.AppendLine("1. Be concise but descriptive");
        prompt.AppendLine("2. Start with a verb in present tense");
        prompt.AppendLine("3. Explain what and why, not how");
        prompt.AppendLine("4. Keep the first line under 50 characters");
        prompt.AppendLine("5. Add more detailed explanation if needed after a blank line");
        
        if (settings.UseConventionalCommits)
        {
            prompt.AppendLine("6. Use conventional commit format");
            prompt.AppendLine("   Types: feat, fix, docs, style, refactor, test, chore");
            
            if (!string.IsNullOrEmpty(settings.Type))
            {
                prompt.AppendLine($"   Use type: {settings.Type}");
            }
            
            if (!string.IsNullOrEmpty(settings.Scope))
            {
                prompt.AppendLine($"   Use scope: {settings.Scope}");
            }
        }
        
        if (settings.BreakingChange)
        {
            prompt.AppendLine("7. Include BREAKING CHANGE note");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("Return ONLY the commit message, no additional text.");
        
        var response = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt.ToString()))
        {
            response.Append(chunk);
        }
        
        return response.ToString().Trim();
    }

    private async Task<string> EditCommitMessage(string initialMessage)
    {
        AnsiConsole.MarkupLine("\n[cyan]Review and edit the commit message:[/]");
        AnsiConsole.WriteLine();
        
        var panel = new Panel(new Text(initialMessage))
        {
            Header = new PanelHeader(" Generated Message "),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);
        
        var choices = new[]
        {
            "Use as is",
            "Edit message",
            "Regenerate",
            "Cancel"
        };
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .AddChoices(choices));
        
        switch (choice)
        {
            case "Use as is":
                return initialMessage;
                
            case "Edit message":
                return AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter commit message:")
                        .DefaultValue(initialMessage));
                
            case "Regenerate":
                return await GenerateCommitMessage(new Settings());
                
            default:
                throw new OperationCanceledException("Commit cancelled");
        }
    }

    private string FormatConventionalCommit(string message, Settings settings)
    {
        // If already in conventional format, return as is
        if (System.Text.RegularExpressions.Regex.IsMatch(message, @"^(feat|fix|docs|style|refactor|test|chore)(\(.+\))?: .+"))
        {
            return message;
        }
        
        // Determine type if not specified
        var type = settings.Type;
        if (string.IsNullOrEmpty(type))
        {
            type = DetermineCommitType(message);
        }
        
        // Build conventional commit
        var result = new StringBuilder();
        result.Append(type);
        
        if (!string.IsNullOrEmpty(settings.Scope))
        {
            result.Append($"({settings.Scope})");
        }
        
        if (settings.BreakingChange)
        {
            result.Append("!");
        }
        
        result.Append(": ");
        
        // Ensure first letter after type is lowercase
        var messagePart = message.Trim();
        if (messagePart.Length > 0 && char.IsUpper(messagePart[0]))
        {
            messagePart = char.ToLower(messagePart[0]) + messagePart.Substring(1);
        }
        
        result.Append(messagePart);
        
        if (settings.BreakingChange)
        {
            result.AppendLine();
            result.AppendLine();
            result.AppendLine("BREAKING CHANGE: This commit contains breaking changes");
        }
        
        return result.ToString();
    }

    private string DetermineCommitType(string message)
    {
        var lower = message.ToLower();
        
        if (lower.Contains("fix") || lower.Contains("bug") || lower.Contains("issue"))
            return "fix";
        if (lower.Contains("add") || lower.Contains("new") || lower.Contains("feature") || lower.Contains("implement"))
            return "feat";
        if (lower.Contains("doc") || lower.Contains("readme") || lower.Contains("comment"))
            return "docs";
        if (lower.Contains("format") || lower.Contains("style") || lower.Contains("lint"))
            return "style";
        if (lower.Contains("refactor") || lower.Contains("restructure") || lower.Contains("reorganize"))
            return "refactor";
        if (lower.Contains("test") || lower.Contains("spec"))
            return "test";
        if (lower.Contains("build") || lower.Contains("deploy") || lower.Contains("config"))
            return "chore";
        
        return "chore"; // Default
    }

    private void DisplayCommitPreview(string message, int fileCount)
    {
        var panel = new Panel(new Rows(
            new Markup($"[bold]Message:[/]"),
            new Text(message),
            new Text(""),
            new Markup($"[cyan]Files affected: {fileCount}[/]")
        ))
        {
            Header = new PanelHeader(" Commit Preview "),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
    }
}