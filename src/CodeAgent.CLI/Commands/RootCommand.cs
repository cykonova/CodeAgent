using CodeAgent.CLI.Shell;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CodeAgent.Providers.Ollama;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CodeAgent.CLI.Commands;

[Description("Start CodeAgent interactive shell")]
public class RootCommand : AsyncCommand
{
    private readonly IServiceProvider _serviceProvider;

    public RootCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var console = AnsiConsole.Console;
        
        // Check if provider is configured
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        var currentProvider = configService.GetValue("DefaultProvider");
        var hasProvider = !string.IsNullOrWhiteSpace(currentProvider);

        if (!hasProvider)
        {
            // Show setup prompt for first-time users
            console.WriteLine("Welcome to CodeAgent! No LLM provider is configured.");
            console.WriteLine("Starting setup wizard...");
            console.WriteLine();
            
            var setupCommand = new SetupCommand(_serviceProvider);
            await setupCommand.ExecuteAsync(context);
        }
        else
        {
            // Quick validation without breaking the build
            await ValidateProviderQuietly();
        }

        // Start interactive shell
        var historyFile = GetHistoryFilePath();
        var shellSettings = new ShellSettings();
        
        var shell = new InteractiveShell(
            "CodeAgent$ ",
            shellSettings.History,
            context.Data as Spectre.Console.Cli.ICommandApp ?? throw new InvalidOperationException("CommandApp not available"),
            _serviceProvider,
            shellSettings);

        return await shell.ShowAsync(console, CancellationToken.None);
    }

    private async Task ValidateProviderQuietly()
    {
        try
        {
            var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
            var factory = _serviceProvider.GetRequiredService<ILLMProviderFactory>();
            var console = AnsiConsole.Console;
            
            var currentProvider = configService.GetValue("DefaultProvider");
            if (string.IsNullOrWhiteSpace(currentProvider)) return;

            // Fix broken model configuration if "list" is set as model
            if (currentProvider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
            {
                var ollamaOptions = _serviceProvider.GetRequiredService<IOptions<OllamaOptions>>();
                var currentModel = ollamaOptions.Value.DefaultModel ?? "llama3.2";
                
                if (currentModel.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    console.MarkupLine("[yellow]Fixing broken model configuration...[/]");
                    await configService.SetValueAsync("Ollama:DefaultModel", "llama3.2");
                    console.MarkupLine("[green]Model reset to llama3.2[/]");
                }
            }

            // Simple validation without complex UI
            var provider = factory.GetProvider(currentProvider);
            if (!provider.IsConfigured)
            {
                console.MarkupLine($"[yellow]Provider '{currentProvider}' needs configuration. Run '/setup' to configure.[/]");
            }
            else
            {
                console.MarkupLine($"[dim]Provider: {currentProvider}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.Console.MarkupLine($"[dim]Startup check failed: {ex.Message}[/]");
        }
    }

    private string GetHistoryFilePath()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var codeAgentDir = Path.Combine(homeDir, ".codeagent");
        
        if (!Directory.Exists(codeAgentDir))
        {
            Directory.CreateDirectory(codeAgentDir);
        }
        
        return Path.Combine(codeAgentDir, "history.txt");
    }
}