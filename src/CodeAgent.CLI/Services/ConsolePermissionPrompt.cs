using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;

namespace CodeAgent.CLI.Services;

public class ConsolePermissionPrompt : IPermissionPrompt
{
    private readonly IAnsiConsole _console;

    public ConsolePermissionPrompt(IAnsiConsole console)
    {
        _console = console;
    }

    public Task<bool> PromptForPermissionAsync(string operation, string path, string? details = null)
    {
        // Build permission prompt
        var prompt = new StringBuilder();
        prompt.AppendLine();
        prompt.AppendLine($"[yellow]Permission Request[/]");
        prompt.AppendLine($"[bold]Operation:[/] {operation}");
        prompt.AppendLine($"[bold]Path:[/] {path}");
        
        if (!string.IsNullOrEmpty(details))
        {
            prompt.AppendLine($"[bold]Details:[/] {details}");
        }
        
        _console.Markup(prompt.ToString());
        
        var confirm = _console.Prompt(
            new ConfirmationPrompt("[yellow]Allow this operation?[/]")
                .ShowDefaultValue(false)
                .ShowChoices(true));
        
        return Task.FromResult(confirm);
    }
}