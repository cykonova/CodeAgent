using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CodeAgent.CLI.Commands;

public class ProviderCommand : AsyncCommand<ProviderCommand.Settings>
{
    private readonly IServiceProvider _serviceProvider;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[ACTION]")]
        [Description("Action to perform (list, select, status)")]
        public string Action { get; set; } = "list";

        [CommandArgument(1, "[PROVIDER]")]
        [Description("Provider name for select action")]
        public string? Provider { get; set; }
    }

    public ProviderCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var console = AnsiConsole.Console;
        var factory = _serviceProvider.GetRequiredService<ILLMProviderFactory>();
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();

        switch (settings.Action.ToLower())
        {
            case "list":
                console.MarkupLine("[bold]Available providers:[/]");
                foreach (var provider in factory.GetAvailableProviders())
                {
                    var current = configService.GetValue("DefaultProvider") == provider ? " [green](current)[/]" : "";
                    console.MarkupLine($"  • {provider}{current}");
                }
                break;

            case "select":
                if (string.IsNullOrWhiteSpace(settings.Provider))
                {
                    console.MarkupLine("[red]Please specify a provider name.[/]");
                    return 1;
                }

                if (factory.GetAvailableProviders().Contains(settings.Provider.ToLower()))
                {
                    await configService.SetValueAsync("DefaultProvider", settings.Provider.ToLower());
                    console.MarkupLine($"[green]Provider set to: {settings.Provider}[/]");
                }
                else
                {
                    console.MarkupLine($"[red]Unknown provider: {settings.Provider}[/]");
                    console.MarkupLine($"[yellow]Available: {string.Join(", ", factory.GetAvailableProviders())}[/]");
                    return 1;
                }
                break;

            case "status":
                var currentProvider = configService.GetValue("DefaultProvider");
                if (!string.IsNullOrWhiteSpace(currentProvider))
                {
                    try
                    {
                        var provider = factory.GetProvider(currentProvider);
                        var isConfigured = provider.IsConfigured;
                        var status = isConfigured ? "[green]Configured[/]" : "[red]Not configured[/]";
                        
                        console.MarkupLine($"[bold]Current provider:[/] {currentProvider}");
                        console.MarkupLine($"[bold]Status:[/] {status}");
                        
                        if (isConfigured)
                        {
                            var isValid = await provider.ValidateConnectionAsync();
                            var connectionStatus = isValid ? "[green]Connected[/]" : "[red]Connection failed[/]";
                            console.MarkupLine($"[bold]Connection:[/] {connectionStatus}");
                        }
                    }
                    catch (Exception ex)
                    {
                        console.MarkupLine($"[red]Error checking provider status: {ex.Message}[/]");
                        return 1;
                    }
                }
                else
                {
                    console.MarkupLine("[yellow]No provider configured. Run /setup to configure.[/]");
                }
                break;

            default:
                console.MarkupLine($"[red]Unknown action: {settings.Action}[/]");
                console.MarkupLine("[yellow]Available actions: list, select, status[/]");
                return 1;
        }

        return 0;
    }
}