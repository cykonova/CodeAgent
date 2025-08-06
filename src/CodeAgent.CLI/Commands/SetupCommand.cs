using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace CodeAgent.CLI.Commands;

public class SetupCommand
{
    private readonly IServiceProvider _serviceProvider;

    public SetupCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync()
    {
        AnsiConsole.Clear();
        
        // Display setup header
        var rule = new Rule("[bold blue]CodeAgent Setup Wizard[/]")
        {
            Style = Style.Parse("blue")
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
        
        // Provider selection
        var provider = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select your [green]LLM provider[/]:")
                .PageSize(10)
                .AddChoices(new[] { "OpenAI", "Claude (Anthropic)", "Ollama (Local)", "Skip Setup" }));

        if (provider == "Skip Setup")
        {
            AnsiConsole.MarkupLine("[yellow]Setup skipped. You can run 'codeagent setup' anytime to configure.[/]");
            return;
        }

        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        
        switch (provider)
        {
            case "OpenAI":
                await SetupOpenAI(configService);
                break;
            case "Claude (Anthropic)":
                await SetupClaude(configService);
                break;
            case "Ollama (Local)":
                await SetupOllama(configService);
                break;
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓ Setup complete![/]");
        AnsiConsole.MarkupLine("[dim]Configuration saved to appsettings.json[/]");
    }

    private async Task SetupOpenAI(IConfigurationService configService)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]OpenAI Configuration[/]");
        AnsiConsole.MarkupLine("[dim]Get your API key from: https://platform.openai.com/api-keys[/]");
        AnsiConsole.WriteLine();

        var apiKey = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your OpenAI [green]API key[/]:")
                .PromptStyle("green")
                .Secret());

        var model = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the [green]model[/] to use:")
                .DefaultValue("gpt-3.5-turbo")
                .PromptStyle("green")
                .ShowDefaultValue());

        await configService.SetValueAsync("DefaultProvider", "openai");
        await configService.SetValueAsync("OpenAI:ApiKey", apiKey);
        await configService.SetValueAsync("OpenAI:DefaultModel", model);
        
        // Test connection
        await TestConnection("openai");
    }

    private async Task SetupClaude(IConfigurationService configService)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Claude (Anthropic) Configuration[/]");
        AnsiConsole.MarkupLine("[dim]Get your API key from: https://console.anthropic.com/[/]");
        AnsiConsole.WriteLine();

        var apiKey = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your Claude [green]API key[/]:")
                .PromptStyle("green")
                .Secret());

        var model = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the [green]model[/] to use:")
                .DefaultValue("claude-3-5-sonnet-20241022")
                .PromptStyle("green")
                .ShowDefaultValue());

        await configService.SetValueAsync("DefaultProvider", "claude");
        await configService.SetValueAsync("Claude:ApiKey", apiKey);
        await configService.SetValueAsync("Claude:DefaultModel", model);
        
        // Test connection
        await TestConnection("claude");
    }

    private async Task SetupOllama(IConfigurationService configService)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Ollama (Local) Configuration[/]");
        AnsiConsole.MarkupLine("[dim]Make sure Ollama is running locally. Install from: https://ollama.ai[/]");
        AnsiConsole.WriteLine();

        // Show default values in muted color
        var baseUrl = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter Ollama [green]base URL[/]:")
                .DefaultValue("http://localhost:11434")
                .PromptStyle("green")
                .ShowDefaultValue()
                .AllowEmpty());

        var model = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the [green]model[/] to use:")
                .DefaultValue("llama3.2")
                .PromptStyle("green")
                .ShowDefaultValue()
                .AllowEmpty());

        // Use defaults if user just presses enter
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "http://localhost:11434";
        if (string.IsNullOrWhiteSpace(model))
            model = "llama3.2";

        await configService.SetValueAsync("DefaultProvider", "ollama");
        await configService.SetValueAsync("Ollama:BaseUrl", baseUrl);
        await configService.SetValueAsync("Ollama:DefaultModel", model);
        
        // Test connection and list available models
        await TestOllamaConnection(baseUrl);
    }

    private async Task TestConnection(string provider)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[yellow]Testing connection...[/]", async ctx =>
            {
                await Task.Delay(1000); // Simulate connection test
                
                try
                {
                    var factory = _serviceProvider.GetRequiredService<Core.Services.ILLMProviderFactory>();
                    var llmProvider = factory.GetProvider(provider);
                    
                    var isValid = await llmProvider.ValidateConnectionAsync();
                    
                    if (isValid)
                    {
                        AnsiConsole.MarkupLine("[green]✓ Connection successful![/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Connection failed. Please check your configuration.[/]");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
                }
            });
    }

    private async Task TestOllamaConnection(string baseUrl)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[yellow]Checking Ollama connection...[/]", async ctx =>
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync($"{baseUrl}/api/tags");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        AnsiConsole.MarkupLine("[green]✓ Connected to Ollama![/]");
                        
                        // Parse and display available models
                        if (content.Contains("models"))
                        {
                            AnsiConsole.MarkupLine("[dim]Available models: Check with 'ollama list'[/]");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Cannot connect to Ollama. Make sure it's running.[/]");
                        AnsiConsole.MarkupLine("[yellow]Install Ollama from: https://ollama.ai[/]");
                        AnsiConsole.MarkupLine("[yellow]Then run: ollama serve[/]");
                    }
                }
                catch (Exception)
                {
                    AnsiConsole.MarkupLine("[red]✗ Cannot connect to Ollama at {0}[/]", baseUrl);
                    AnsiConsole.MarkupLine("[yellow]Make sure Ollama is installed and running.[/]");
                }
            });
    }
}