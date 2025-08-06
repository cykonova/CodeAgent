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
        [CommandArgument(0, "[ACTION_OR_MODEL]")]
        [Description("Action (list) or model name to switch to")]
        public string? ActionOrModel { get; set; }
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

        // Get current model
        var ollamaOptions = _serviceProvider.GetRequiredService<IOptions<OllamaOptions>>();
        var currentModel = ollamaOptions.Value.DefaultModel ?? "llama3.2";

        if (string.IsNullOrWhiteSpace(settings.ActionOrModel))
        {
            // Show interactive selection prompt
            var availableModels = await GetAvailableModelsAsync();
            if (availableModels.Any())
            {
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Select Ollama model:[/]")
                        .AddChoices(availableModels)
                        .HighlightStyle(new Style(foreground: Color.Cyan1))
                );
                
                await configService.SetValueAsync("Ollama:DefaultModel", selection);
                console.MarkupLine($"[green]Ollama model set to: {selection}[/]");
            }
            else
            {
                console.MarkupLine($"[bold]Current Ollama model:[/] {currentModel}");
                console.MarkupLine("[dim]Use '/model list' to see available models[/]");
                console.MarkupLine("[dim]Use '/model <model-name>' to change model[/]");
                console.MarkupLine("[red]No models found. Make sure Ollama is running and has models installed.[/]");
            }
        }
        else if (settings.ActionOrModel.Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            // List available models
            console.MarkupLine($"[bold]Current model:[/] {currentModel}");
            console.WriteLine();
            
            var availableModels = await GetAvailableModelsAsync();
            if (availableModels.Any())
            {
                console.MarkupLine("[bold]Available models:[/]");
                foreach (var model in availableModels)
                {
                    var current = model == currentModel ? " [green](current)[/]" : "";
                    console.MarkupLine($"  • {model}{current}");
                }
            }
            else
            {
                console.MarkupLine("[red]No models found. Make sure Ollama is running and has models installed.[/]");
                console.MarkupLine("[dim]Install models with: ollama pull <model-name>[/]");
            }
        }
        else
        {
            // Set specific model
            var modelName = settings.ActionOrModel;
            var availableModels = await GetAvailableModelsAsync();
            
            if (availableModels.Contains(modelName))
            {
                await configService.SetValueAsync("Ollama:DefaultModel", modelName);
                console.MarkupLine($"[green]Ollama model set to: {modelName}[/]");
            }
            else
            {
                console.MarkupLine($"[red]Model '{modelName}' not found.[/]");
                if (availableModels.Any())
                {
                    console.MarkupLine($"[yellow]Available models: {string.Join(", ", availableModels)}[/]");
                }
                console.MarkupLine($"[dim]Install with: ollama pull {modelName}[/]");
                return 1;
            }
        }

        return 0;
    }

    private async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            // Call Ollama API to get available models
            var httpClient = _serviceProvider.GetService<HttpClient>() ?? new HttpClient();
            var ollamaOptions = _serviceProvider.GetRequiredService<IOptions<OllamaOptions>>();
            var baseUrl = ollamaOptions.Value.BaseUrl?.TrimEnd('/') ?? "http://localhost:11434";
            
            var response = await httpClient.GetAsync($"{baseUrl}/api/tags");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var models = System.Text.Json.JsonSerializer.Deserialize<OllamaModelsResponse>(content);
                return models?.Models?.Select(m => m.Name.Split(':')[0]).Distinct().OrderBy(x => x).ToList() ?? new List<string>();
            }
        }
        catch
        {
            // Fallback to common models if API call fails
        }
        
        return new List<string> { "llama3.2", "llama3.1", "codellama", "mistral", "phi3", "qwen2", "gemma2" };
    }

    private class OllamaModelsResponse
    {
        public List<OllamaModel> Models { get; set; } = new();
    }

    private class OllamaModel
    {
        public string Name { get; set; } = string.Empty;
    }
}