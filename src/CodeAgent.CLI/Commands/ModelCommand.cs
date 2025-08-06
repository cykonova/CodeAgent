using CodeAgent.Domain.Interfaces;
using CodeAgent.Providers.Ollama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CodeAgent.CLI.Commands;

public class ModelCommand : AsyncCommand<ModelCommand.Settings>
{
    private readonly IServiceProvider _serviceProvider;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[MODEL_NAME]")]
        [Description("Model name to switch to (Ollama only)")]
        public string? ModelName { get; set; }
    }

    public ModelCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var console = AnsiConsole.Console;
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        
        var currentProvider = configService.GetValue("DefaultProvider");
        if (currentProvider?.Equals("ollama", StringComparison.OrdinalIgnoreCase) != true)
        {
            console.MarkupLine("[red]Model switching is only available for Ollama provider.[/]");
            console.MarkupLine("[yellow]Current provider: {0}[/]", currentProvider ?? "None");
            console.MarkupLine("[dim]Use '/provider ollama' to switch to Ollama first.[/]");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.ModelName))
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
            await configService.SetValueAsync("Ollama:DefaultModel", settings.ModelName);
            console.MarkupLine($"[green]Ollama model set to: {settings.ModelName}[/]");
            console.MarkupLine("[dim]Note: Make sure the model is available in your Ollama installation.[/]");
        }

        return 0;
    }
}