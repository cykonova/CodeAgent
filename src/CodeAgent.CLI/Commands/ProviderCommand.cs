using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Providers.Ollama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        [Description("Action to perform (list, select, status, model)")]
        public string Action { get; set; } = "list";

        [CommandArgument(1, "[PROVIDER_OR_MODEL]")]
        [Description("Provider name for select action or model name for model action")]
        public string? ProviderOrModel { get; set; }
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
        var availableProviders = factory.GetAvailableProviders().ToList();

        // Check if the action is actually a provider name
        if (availableProviders.Contains(settings.Action.ToLower()))
        {
            // Treat as provider selection
            await configService.SetValueAsync("DefaultProvider", settings.Action.ToLower());
            console.MarkupLine($"[green]Provider set to: {settings.Action.ToLower()}[/]");
            return 0;
        }

        switch (settings.Action.ToLower())
        {
            case "list":
                console.MarkupLine("[bold]Available providers:[/]");
                foreach (var provider in availableProviders)
                {
                    var current = configService.GetValue("DefaultProvider") == provider ? " [green](current)[/]" : "";
                    console.MarkupLine($"  • {provider}{current}");
                }
                break;

            case "select":
                if (string.IsNullOrWhiteSpace(settings.ProviderOrModel))
                {
                    console.MarkupLine("[red]Please specify a provider name.[/]");
                    return 1;
                }

                if (availableProviders.Contains(settings.ProviderOrModel.ToLower()))
                {
                    await configService.SetValueAsync("DefaultProvider", settings.ProviderOrModel.ToLower());
                    console.MarkupLine($"[green]Provider set to: {settings.ProviderOrModel}[/]");
                }
                else
                {
                    console.MarkupLine($"[red]Unknown provider: {settings.ProviderOrModel}[/]");
                    console.MarkupLine($"[yellow]Available: {string.Join(", ", availableProviders)}[/]");
                    return 1;
                }
                break;

            case "status":
                var currentProvider = configService.GetValue("DefaultProvider");
                if (!string.IsNullOrWhiteSpace(currentProvider))
                {
                    try
                    {
                        var providerInstance = factory.GetProvider(currentProvider);
                        var isConfigured = providerInstance.IsConfigured;
                        var status = isConfigured ? "[green]Configured[/]" : "[red]Not configured[/]";
                        
                        console.MarkupLine($"[bold]Current provider:[/] {currentProvider}");
                        console.MarkupLine($"[bold]Status:[/] {status}");
                        
                        // Show model information for Ollama
                        if (currentProvider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                        {
                            var ollamaOptions = _serviceProvider.GetRequiredService<IOptions<OllamaOptions>>();
                            var currentModel = ollamaOptions.Value.DefaultModel ?? "llama3.2";
                            console.MarkupLine($"[bold]Current model:[/] {currentModel}");
                        }
                        
                        if (isConfigured)
                        {
                            var isValid = await providerInstance.ValidateConnectionAsync();
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

            case "model":
                var providerName = configService.GetValue("DefaultProvider");
                if (providerName?.Equals("ollama", StringComparison.OrdinalIgnoreCase) != true)
                {
                    console.MarkupLine("[red]Model switching is only available for Ollama provider.[/]");
                    console.MarkupLine("[yellow]Current provider: {0}[/]", providerName ?? "None");
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(settings.ProviderOrModel))
                {
                    // Show current model
                    var ollamaOptions = _serviceProvider.GetRequiredService<IOptions<OllamaOptions>>();
                    var currentModel = ollamaOptions.Value.DefaultModel ?? "llama3.2";
                    console.MarkupLine($"[bold]Current Ollama model:[/] {currentModel}");
                    console.MarkupLine("[dim]Use '/model <model-name>' to change model[/]");
                    console.MarkupLine("[dim]Popular models: llama3.2, llama3.1, codellama, mistral, etc.[/]");
                }
                else
                {
                    // Set new model
                    await configService.SetValueAsync("Ollama:DefaultModel", settings.ProviderOrModel);
                    console.MarkupLine($"[green]Ollama model set to: {settings.ProviderOrModel}[/]");
                    console.MarkupLine("[dim]Note: Make sure the model is available in your Ollama installation.[/]");
                }
                break;

            default:
                console.MarkupLine($"[red]Unknown action: {settings.Action}[/]");
                console.MarkupLine($"[yellow]Available actions: list, select, status, model[/]");
                console.MarkupLine($"[yellow]Available providers: {string.Join(", ", availableProviders)}[/]");
                return 1;
        }

        return 0;
    }
}