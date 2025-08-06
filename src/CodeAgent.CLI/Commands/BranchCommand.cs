using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class BranchCommand : AsyncCommand<BranchCommand.Settings>
{
    private readonly IGitService _gitService;
    private readonly IChatService _chatService;

    public BranchCommand(IGitService gitService, IChatService chatService)
    {
        _gitService = gitService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("Branch name or action")]
        [CommandArgument(0, "[name]")]
        public string? Name { get; set; }

        [Description("List all branches")]
        [CommandOption("-l|--list")]
        public bool List { get; set; }

        [Description("Create new branch")]
        [CommandOption("-c|--create")]
        public bool Create { get; set; }

        [Description("Delete branch")]
        [CommandOption("-d|--delete")]
        public bool Delete { get; set; }

        [Description("Switch to branch")]
        [CommandOption("-s|--switch")]
        public bool Switch { get; set; }

        [Description("Auto-generate branch name from task")]
        [CommandOption("--auto-name")]
        public bool AutoName { get; set; }

        [Description("Branch type (feature, bugfix, hotfix, release)")]
        [CommandOption("-t|--type")]
        public BranchType Type { get; set; } = BranchType.Feature;

        [Description("Task or issue number")]
        [CommandOption("--task")]
        public string? Task { get; set; }

        [Description("Description for auto-generated name")]
        [CommandOption("--description")]
        public string? Description { get; set; }
    }

    public enum BranchType
    {
        Feature,
        Bugfix,
        Hotfix,
        Release,
        Chore
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold blue]Git Branch Management[/]"));
            
            // Check if we're in a Git repository
            if (!await _gitService.IsRepositoryAsync("."))
            {
                AnsiConsole.MarkupLine("[red]Error: Not in a Git repository[/]");
                return 1;
            }
            
            // Determine action
            if (settings.List || (!settings.Create && !settings.Delete && !settings.Switch && string.IsNullOrEmpty(settings.Name)))
            {
                await ListBranches();
            }
            else if (settings.Create || settings.AutoName)
            {
                await CreateBranch(settings);
            }
            else if (settings.Delete)
            {
                await DeleteBranch(settings);
            }
            else if (settings.Switch || !string.IsNullOrEmpty(settings.Name))
            {
                await SwitchBranch(settings);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task ListBranches()
    {
        var currentBranch = await _gitService.GetCurrentBranchAsync(".");
        var branches = await _gitService.GetBranchesAsync(".");
        
        var tree = new Tree("[bold]Git Branches[/]");
        tree.Style = new Style(Color.Blue);
        
        var localNode = tree.AddNode("[cyan]Local[/]");
        var remoteNode = tree.AddNode("[yellow]Remote[/]");
        
        foreach (var branch in branches)
        {
            var branchName = branch.Trim();
            var isRemote = branchName.StartsWith("remotes/");
            var isCurrent = branchName == currentBranch;
            
            var displayName = branchName;
            if (isRemote)
            {
                displayName = branchName.Replace("remotes/", "");
            }
            
            if (isCurrent)
            {
                displayName = $"[green]● {displayName} (current)[/]";
            }
            
            if (isRemote)
            {
                remoteNode.AddNode(displayName);
            }
            else
            {
                localNode.AddNode(displayName);
            }
        }
        
        AnsiConsole.Write(tree);
        
        // Display branch information
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[cyan]Current branch: {currentBranch}[/]");
        AnsiConsole.MarkupLine($"[cyan]Total branches: {branches.Count()}[/]");
    }

    private async Task CreateBranch(Settings settings)
    {
        string branchName;
        
        if (settings.AutoName)
        {
            branchName = await GenerateBranchName(settings);
            
            // Confirm generated name
            var panel = new Panel(new Text(branchName))
            {
                Header = new PanelHeader(" Generated Branch Name "),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);
            
            if (!AnsiConsole.Confirm("Use this branch name?"))
            {
                branchName = AnsiConsole.Ask<string>("Enter branch name:");
            }
        }
        else if (string.IsNullOrEmpty(settings.Name))
        {
            branchName = AnsiConsole.Ask<string>("Enter branch name:");
        }
        else
        {
            branchName = settings.Name;
        }
        
        // Validate branch name
        if (!IsValidBranchName(branchName))
        {
            AnsiConsole.MarkupLine("[red]Invalid branch name. Branch names cannot contain spaces or special characters.[/]");
            return;
        }
        
        // Create the branch
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"Creating branch '{branchName}'...", async ctx =>
            {
                var success = await _gitService.CreateBranchAsync(".", branchName);
                
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓ Branch '{branchName}' created and checked out[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗ Failed to create branch '{branchName}'[/]");
                }
            });
    }

    private async Task DeleteBranch(Settings settings)
    {
        var branchName = settings.Name;
        
        if (string.IsNullOrEmpty(branchName))
        {
            // Select branch to delete
            var branches = await _gitService.GetBranchesAsync(".");
            var currentBranch = await _gitService.GetCurrentBranchAsync(".");
            
            var selectableBranches = branches
                .Where(b => b != currentBranch && !b.StartsWith("remotes/"))
                .ToList();
            
            if (!selectableBranches.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No branches available to delete[/]");
                return;
            }
            
            branchName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select branch to delete:")
                    .AddChoices(selectableBranches));
        }
        
        // Confirm deletion
        if (!AnsiConsole.Confirm($"Delete branch '{branchName}'?", false))
        {
            AnsiConsole.MarkupLine("[yellow]Deletion cancelled[/]");
            return;
        }
        
        // Note: IGitService doesn't have DeleteBranch method, so we'd need to add it
        AnsiConsole.MarkupLine($"[yellow]Branch deletion not yet implemented in IGitService[/]");
    }

    private async Task SwitchBranch(Settings settings)
    {
        var branchName = settings.Name;
        
        if (string.IsNullOrEmpty(branchName))
        {
            // Select branch to switch to
            var branches = await _gitService.GetBranchesAsync(".");
            var currentBranch = await _gitService.GetCurrentBranchAsync(".");
            
            var selectableBranches = branches
                .Where(b => b != currentBranch)
                .Select(b => b.Replace("remotes/origin/", ""))
                .Distinct()
                .ToList();
            
            if (!selectableBranches.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No other branches available[/]");
                return;
            }
            
            branchName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select branch to switch to:")
                    .AddChoices(selectableBranches));
        }
        
        // Check for uncommitted changes
        if (await _gitService.HasUncommittedChangesAsync("."))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: You have uncommitted changes[/]");
            
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices("Stash changes and switch", "Commit changes first", "Cancel"));
            
            switch (action)
            {
                case "Stash changes and switch":
                    // Note: Would need to implement stash in IGitService
                    AnsiConsole.MarkupLine("[yellow]Stash not yet implemented[/]");
                    return;
                    
                case "Commit changes first":
                    AnsiConsole.MarkupLine("[cyan]Please commit your changes first[/]");
                    return;
                    
                default:
                    AnsiConsole.MarkupLine("[yellow]Switch cancelled[/]");
                    return;
            }
        }
        
        // Switch to the branch
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"Switching to branch '{branchName}'...", async ctx =>
            {
                var success = await _gitService.CheckoutBranchAsync(".", branchName);
                
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✓ Switched to branch '{branchName}'[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗ Failed to switch to branch '{branchName}'[/]");
                }
            });
    }

    private async Task<string> GenerateBranchName(Settings settings)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Generate a Git branch name based on the following:");
        prompt.AppendLine($"Branch Type: {settings.Type}");
        
        if (!string.IsNullOrEmpty(settings.Task))
        {
            prompt.AppendLine($"Task/Issue: {settings.Task}");
        }
        
        if (!string.IsNullOrEmpty(settings.Description))
        {
            prompt.AppendLine($"Description: {settings.Description}");
        }
        else
        {
            // Get context from recent changes
            var diff = await _gitService.GetDiffAsync(".");
            if (!string.IsNullOrEmpty(diff))
            {
                prompt.AppendLine("Recent changes context:");
                prompt.AppendLine(diff.Length > 500 ? diff.Substring(0, 500) + "..." : diff);
            }
        }
        
        prompt.AppendLine();
        prompt.AppendLine("Requirements:");
        prompt.AppendLine("1. Use lowercase and hyphens (no spaces or special characters)");
        prompt.AppendLine("2. Start with the branch type prefix (feature/, bugfix/, hotfix/, release/, chore/)");
        prompt.AppendLine("3. Be descriptive but concise (max 50 characters)");
        prompt.AppendLine("4. Include task number if provided");
        prompt.AppendLine("5. Follow git-flow naming conventions");
        prompt.AppendLine();
        prompt.AppendLine("Return ONLY the branch name, no additional text.");
        
        var response = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt.ToString()))
        {
            response.Append(chunk);
        }
        
        var generatedName = response.ToString().Trim();
        
        // Ensure it starts with the correct prefix
        var prefix = settings.Type.ToString().ToLower() + "/";
        if (!generatedName.StartsWith(prefix))
        {
            generatedName = prefix + generatedName;
        }
        
        // Clean up the name
        generatedName = System.Text.RegularExpressions.Regex.Replace(generatedName, @"[^a-zA-Z0-9\-/]", "-");
        generatedName = System.Text.RegularExpressions.Regex.Replace(generatedName, @"-+", "-");
        generatedName = generatedName.Trim('-');
        
        return generatedName;
    }

    private bool IsValidBranchName(string name)
    {
        // Git branch name validation
        if (string.IsNullOrWhiteSpace(name))
            return false;
        
        // Cannot start or end with certain characters
        if (name.StartsWith(".") || name.StartsWith("-") || name.EndsWith(".") || name.EndsWith(".lock"))
            return false;
        
        // Cannot contain certain characters
        var invalidChars = new[] { ' ', '~', '^', ':', '?', '*', '[', '\\', '@', '{', '}' };
        if (invalidChars.Any(c => name.Contains(c)))
            return false;
        
        // Cannot contain consecutive dots or slashes
        if (name.Contains("..") || name.Contains("//"))
            return false;
        
        return true;
    }
}