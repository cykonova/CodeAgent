using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CodeAgent.CLI.Commands;

public class PromptCommand : AsyncCommand<PromptCommand.Settings>
{
    private readonly IServiceProvider _serviceProvider;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[ACTION]")]
        [Description("Action: show, edit, reset")]
        public string Action { get; set; } = "show";
    }

    public PromptCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var console = AnsiConsole.Console;
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();

        switch (settings.Action.ToLower())
        {
            case "show":
                var currentPrompt = configService.GetValue("SystemPrompt");
                if (string.IsNullOrWhiteSpace(currentPrompt))
                {
                    console.MarkupLine("[yellow]No custom system prompt set. Using default enhanced prompt.[/]");
                    console.MarkupLine("[dim]Use '/prompt edit' to customize the system prompt.[/]");
                }
                else
                {
                    console.MarkupLine("[bold]Current custom system prompt:[/]");
                    console.WriteLine();
                    console.MarkupLine("[dim]" + currentPrompt.Replace("[", "[[").Replace("]", "]]") + "[/]");
                    console.WriteLine();
                    console.MarkupLine("[dim]Use '/prompt reset' to return to default or '/prompt edit' to modify.[/]");
                }
                break;

            case "edit":
                console.MarkupLine("[bold]Enter your custom system prompt:[/]");
                console.MarkupLine("[dim]This will be used instead of the default prompt. Press Ctrl+C to cancel.[/]");
                console.WriteLine();
                
                var newPrompt = await GetMultiLineInput(console);
                if (!string.IsNullOrWhiteSpace(newPrompt))
                {
                    await configService.SetValueAsync("SystemPrompt", newPrompt);
                    console.MarkupLine("[green]Custom system prompt saved![/]");
                    console.MarkupLine("[yellow]Note: Restart the session or use '/clear' for the new prompt to take effect.[/]");
                }
                else
                {
                    console.MarkupLine("[yellow]Operation cancelled.[/]");
                }
                break;

            case "reset":
                var hasCustomPrompt = !string.IsNullOrWhiteSpace(configService.GetValue("SystemPrompt"));
                if (hasCustomPrompt)
                {
                    var confirm = AnsiConsole.Confirm("Reset to default enhanced system prompt?");
                    if (confirm)
                    {
                        await configService.SetValueAsync("SystemPrompt", "");
                        console.MarkupLine("[green]System prompt reset to default.[/]");
                        console.MarkupLine("[yellow]Note: Restart the session or use '/clear' for the change to take effect.[/]");
                    }
                }
                else
                {
                    console.MarkupLine("[yellow]Already using default system prompt.[/]");
                }
                break;

            default:
                console.MarkupLine($"[red]Unknown action: {settings.Action}[/]");
                console.MarkupLine("[yellow]Available actions: show, edit, reset[/]");
                return 1;
        }

        return 0;
    }

    private Task<string> GetMultiLineInput(IAnsiConsole console)
    {
        console.MarkupLine("[dim]Enter your prompt (end with a line containing only 'END'):[/]");
        console.WriteLine();

        var lines = new List<string>();
        string? line;

        while ((line = Console.ReadLine()) != null)
        {
            if (line.Trim().Equals("END", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            lines.Add(line);
        }

        return Task.FromResult(string.Join(Environment.NewLine, lines).Trim());
    }
}