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

    public Task<PermissionResult> PromptForPermissionAsync(string operation, string path, string projectDir, string? details = null)
    {
        // Build permission prompt
        var prompt = new StringBuilder();
        prompt.AppendLine();
        prompt.AppendLine($"[yellow]Permission Request[/]");
        prompt.AppendLine($"[bold]Operation:[/] {operation}");
        prompt.AppendLine($"[bold]Path:[/] {path}");
        prompt.AppendLine($"[bold]Project Directory:[/] {projectDir}");
        
        if (!string.IsNullOrEmpty(details))
        {
            prompt.AppendLine($"[bold]Details:[/] {details}");
        }
        
        _console.Markup(prompt.ToString());
        
        var choice = _console.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Choose an option:[/]")
                .AddChoices(new[] {
                    "Yes - Allow this operation",
                    $"Yes - Allow all '{operation}' operations in project directory",
                    "No - Deny and tell LLM what to do instead"
                }));
        
        return choice switch
        {
            "Yes - Allow this operation" => Task.FromResult(PermissionResult.Allowed),
            var s when s.StartsWith("Yes - Allow all") => Task.FromResult(PermissionResult.AllowedForAll),
            _ => Task.FromResult(PermissionResult.Denied)
        };
    }
}