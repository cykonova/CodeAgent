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
        
        // Launch web portal in daemon mode
        console.WriteLine("[green]Starting CodeAgent Web Portal...[/]");
        console.WriteLine("[dim]The web interface will open in your browser at http://localhost:5001[/]");
        console.WriteLine("[dim]Press Ctrl+C to stop the server[/]");
        
        // Start the web application from the correct directory
        var currentDir = Directory.GetCurrentDirectory();
        var webProjectPath = Path.Combine(currentDir, "src", "CodeAgent.Web");
        
        if (!Directory.Exists(webProjectPath))
        {
            // Try finding it relative to the executable
            var baseDir = AppContext.BaseDirectory;
            webProjectPath = Path.Combine(baseDir, "..", "..", "..", "..", "CodeAgent.Web");
        }
        
        if (!Directory.Exists(webProjectPath))
        {
            console.WriteLine("[red]Error: Could not find CodeAgent.Web project directory[/]");
            console.WriteLine("[yellow]Please ensure you're running from the solution root.[/]");
            return 1;
        }
        
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --urls \"http://localhost:5001\"",
            WorkingDirectory = webProjectPath,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        
        try
        {
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                console.WriteLine("[red]Failed to start web server[/]");
                return 1;
            }
            
            // Wait for process to exit or Ctrl+C
            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            console.WriteLine($"[red]Error starting web server: {ex.Message}[/]");
            return 1;
        }
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