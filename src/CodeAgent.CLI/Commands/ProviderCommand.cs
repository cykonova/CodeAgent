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
        [Description("Action to perform (list, select, status, model) or leave empty for interactive selection")]
        public string Action { get; set; } = "";

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

        // If no action provided, show interactive selection
        if (string.IsNullOrWhiteSpace(settings.Action) || settings.Action.Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            var currentProvider = configService.GetValue("DefaultProvider");
            var choices = availableProviders.ToList();
            
            // Add current selection indicator
            var choicesWithStatus = choices.Select(p => 
                p == currentProvider ? $"{p} (current)" : p
            ).ToList();

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select LLM provider:[/]")
                    .AddChoices(choicesWithStatus)
                    .HighlightStyle(new Style(foreground: Color.Cyan1))
            );
            
            // Remove the " (current)" suffix if present
            var selectedProvider = selection.Replace(" (current)", "");
            
            if (selectedProvider != currentProvider)
            {
                await configService.SetValueAsync("DefaultProvider", selectedProvider);
                console.MarkupLine($"[green]Provider set to: {selectedProvider}[/]");
                
                // Validate the provider after selection
                await ValidateProviderAndModel(console, factory, configService, selectedProvider);
            }
            else
            {
                console.MarkupLine($"[yellow]Provider unchanged: {selectedProvider}[/]");
            }
            
            return 0;
        }

        // Check if the action is actually a provider name
        if (availableProviders.Contains(settings.Action.ToLower()))
        {
            // Treat as provider selection
            await configService.SetValueAsync("DefaultProvider", settings.Action.ToLower());
            console.MarkupLine($"[green]Provider set to: {settings.Action.ToLower()}[/]");
            
            // Validate the provider after selection
            await ValidateProviderAndModel(console, factory, configService, settings.Action.ToLower());
            return 0;
        }

        switch (settings.Action.ToLower())
        {
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
                    
                    // Validate the provider after selection
                    await ValidateProviderAndModel(console, factory, configService, settings.ProviderOrModel.ToLower());
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
                    await ShowProviderStatus(console, factory, configService, currentProvider);
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
                    console.MarkupLine("[dim]Use '/model' for interactive selection or '/model list' to see available models[/]");
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

    private async Task ShowProviderStatus(IAnsiConsole console, ILLMProviderFactory factory, IConfigurationService configService, string providerName)
    {
        try
        {
            var providerInstance = factory.GetProvider(providerName);
            var isConfigured = providerInstance.IsConfigured;
            var status = isConfigured ? "[green]Configured[/]" : "[red]Not configured[/]";
            
            console.MarkupLine($"[bold]Current provider:[/] {providerName}");
            console.MarkupLine($"[bold]Status:[/] {status}");
            
            // Show model information for Ollama
            if (providerName.Equals("ollama", StringComparison.OrdinalIgnoreCase))
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
        }
    }

    private async Task ValidateProviderAndModel(IAnsiConsole console, ILLMProviderFactory factory, IConfigurationService configService, string providerName)
    {
        try
        {
            var provider = factory.GetProvider(providerName);
            
            if (!provider.IsConfigured)
            {
                console.MarkupLine($"[yellow]Provider '{providerName}' is not configured.[/]");
                console.MarkupLine("[dim]Run '/setup' to configure API keys and settings.[/]");
                return;
            }
            
            var isConnected = await provider.ValidateConnectionAsync();
            if (!isConnected)
            {
                console.MarkupLine($"[red]Cannot connect to '{providerName}'. Please check your configuration.[/]");
                console.MarkupLine("[dim]Run '/setup' to reconfigure or check your internet connection.[/]");
                return;
            }
            
            // For Ollama, also validate the model
            if (providerName.Equals("ollama", StringComparison.OrdinalIgnoreCase))
            {
                var ollamaOptions = _serviceProvider.GetRequiredService<IOptions<OllamaOptions>>();
                var currentModel = ollamaOptions.Value.DefaultModel ?? "llama3.2";
                
                // Check if model is available (this would need to be implemented in ModelCommand)
                console.MarkupLine($"[green]Connected to Ollama. Current model: {currentModel}[/]");
                console.MarkupLine("[dim]Use '/model list' to see available models.[/]");
            }
            else
            {
                console.MarkupLine($"[green]Successfully connected to {providerName}![/]");
            }
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error validating provider: {ex.Message}[/]");
        }
    }
}