using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CodeAgent.CLI.Commands;

[Description("Switch between different LLM providers")]
public class SwitchProviderCommand : AsyncCommand<SwitchProviderCommand.Settings>
{
    private readonly IProviderManager _providerManager;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[provider]")]
        [Description("Provider name to switch to (leave empty to select interactively)")]
        public string? ProviderName { get; set; }

        [CommandOption("-l|--list")]
        [Description("List all available providers")]
        public bool ListProviders { get; set; }

        [CommandOption("-t|--test")]
        [Description("Test provider connectivity")]
        public bool TestProvider { get; set; }
    }

    public SwitchProviderCommand(IProviderManager providerManager)
    {
        _providerManager = providerManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.ListProviders)
        {
            ListAvailableProviders();
            return 0;
        }

        var providerName = settings.ProviderName;
        
        if (string.IsNullOrEmpty(providerName))
        {
            // Interactive selection
            var providers = _providerManager.GetAvailableProviders();
            if (!providers.Any())
            {
                AnsiConsole.MarkupLine("[red]No providers available[/]");
                return 1;
            }

            providerName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]provider[/]:")
                    .AddChoices(providers)
                    .HighlightStyle(new Style(foreground: Color.Aqua)));
        }

        if (settings.TestProvider)
        {
            return await TestProviderConnection(providerName);
        }

        return await SwitchToProvider(providerName);
    }

    private void ListAvailableProviders()
    {
        var providers = _providerManager.GetAvailableProviders();
        var currentProvider = _providerManager.CurrentProviderName;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Provider")
            .AddColumn("Status")
            .AddColumn("Capabilities");

        foreach (var provider in providers)
        {
            var status = provider == currentProvider ? "[green]Active[/]" : "[gray]Available[/]";
            var capabilities = _providerManager.GetProviderCapabilities(provider);
            
            var capsText = "";
            if (capabilities != null)
            {
                var caps = new List<string>();
                if (capabilities.SupportsStreaming) caps.Add("Streaming");
                if (capabilities.SupportsFunctionCalling) caps.Add("Functions");
                if (capabilities.SupportsVision) caps.Add("Vision");
                if (capabilities.SupportsEmbeddings) caps.Add("Embeddings");
                capsText = string.Join(", ", caps);
            }

            table.AddRow(provider, status, capsText);
        }

        AnsiConsole.Write(table);
    }

    private async Task<int> TestProviderConnection(string providerName)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Testing provider [cyan]{providerName}[/]...", async ctx =>
            {
                var isAvailable = await _providerManager.TestProviderAsync(providerName);
                
                if (isAvailable)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Provider [cyan]{providerName}[/] is available");
                    
                    var capabilities = _providerManager.GetProviderCapabilities(providerName);
                    if (capabilities != null)
                    {
                        AnsiConsole.MarkupLine($"  Models: {string.Join(", ", capabilities.AvailableModels)}");
                        AnsiConsole.MarkupLine($"  Max tokens: {capabilities.MaxTokens:N0}");
                        AnsiConsole.MarkupLine($"  Context length: {capabilities.MaxContextLength:N0}");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Provider [cyan]{providerName}[/] is not available");
                }
            });

        return 0;
    }

    private async Task<int> SwitchToProvider(string providerName)
    {
        var success = await _providerManager.SwitchProviderAsync(providerName);
        
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Switched to provider [cyan]{providerName}[/]");
            
            var capabilities = _providerManager.GetProviderCapabilities(providerName);
            if (capabilities != null && capabilities.AvailableModels.Any())
            {
                AnsiConsole.MarkupLine($"  Available models: {string.Join(", ", capabilities.AvailableModels.Take(3))}...");
            }
            
            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Failed to switch to provider [cyan]{providerName}[/]");
            AnsiConsole.MarkupLine("[yellow]Provider may not be configured or available[/]");
            return 1;
        }
    }
}