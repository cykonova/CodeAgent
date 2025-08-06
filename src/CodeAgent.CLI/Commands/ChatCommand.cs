using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using CodeAgent.Domain.Interfaces;

namespace CodeAgent.CLI.Commands;

public class ChatCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IChatService _chatService;
    private readonly ILLMProvider _llmProvider;

    public ChatCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _chatService = _serviceProvider.GetRequiredService<IChatService>();
        _llmProvider = _serviceProvider.GetRequiredService<ILLMProvider>();
    }

    public async Task ExecuteAsync()
    {
        if (!_llmProvider.IsConfigured)
        {
            AnsiConsole.MarkupLine("[red]LLM provider is not configured. Please set up your API key.[/]");
            await ConfigureProviderAsync();
        }

        AnsiConsole.MarkupLine("[green]Starting chat session. Type 'exit' to quit, 'clear' to clear history.[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var input = AnsiConsole.Ask<string>("[bold cyan]You:[/] ");
            
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                break;
            }

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                _chatService.ClearHistory();
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[green]Chat history cleared.[/]");
                continue;
            }

            AnsiConsole.MarkupLine("[bold green]Assistant:[/] ");
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Thinking...", async ctx =>
                {
                    try
                    {
                        var response = await _chatService.ProcessMessageAsync(input);
                        
                        ctx.Status("Generating response...");
                        
                        if (!string.IsNullOrEmpty(response.Error))
                        {
                            AnsiConsole.MarkupLine($"[red]Error: {response.Error}[/]");
                        }
                        else
                        {
                            AnsiConsole.WriteLine(response.Content);
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    }
                });
            
            AnsiConsole.WriteLine();
        }
    }

    private async Task ConfigureProviderAsync()
    {
        var apiKey = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Enter your OpenAI API key:[/]")
                .PromptStyle("green")
                .Secret());

        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        configService.SetValue("LLMProvider:ApiKey", apiKey);
        await configService.SaveAsync();
        
        AnsiConsole.MarkupLine("[green]Configuration saved![/]");
    }
}