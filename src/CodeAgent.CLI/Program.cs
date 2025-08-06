using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using CodeAgent.CLI;
using CodeAgent.CLI.Commands;
using CodeAgent.CLI.Services;
using CodeAgent.CLI.Shell;
using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Infrastructure.Services;
using CodeAgent.Providers.OpenAI;
using CodeAgent.Providers.Claude;
using CodeAgent.Providers.Ollama;
using CodeAgent.MCP;
using Spectre.Console;
using Spectre.Console.Cli;

// Set up paths
var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var codeAgentDir = Path.Combine(homeDir, ".codeagent");
Directory.CreateDirectory(codeAgentDir);
var historyFile = Path.Combine(codeAgentDir, "history");
var settingsPath = Path.Combine(codeAgentDir, "settings.json");

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(settingsPath, optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

// Create the CLI host
var builder = new CliHostBuilder();

// Configure services
builder.ConfigureServices(services =>
{
    // Configuration
    services.AddSingleton<IConfiguration>(configuration);
    services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
    services.Configure<ClaudeOptions>(configuration.GetSection("Claude"));
    services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
    services.Configure<MCPOptions>(configuration.GetSection("MCP"));
    
    // Logging
    services.AddLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning);
    });
    
    // HTTP Client
    services.AddHttpClient();
    
    // Console
    services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
    services.AddSingleton<IPermissionPrompt, ConsolePermissionPrompt>();
    
    // Core services
    services.AddSingleton<IConfigurationService, ConfigurationService>();
    services.AddSingleton<IFileSystemService, FileSystemService>();
    services.AddSingleton<IDiffService, DiffService>();
    services.AddSingleton<IContextService, ContextService>();
    services.AddSingleton<IRetryService, RetryService>();
    services.AddSingleton<IPermissionService, PermissionService>();
    services.AddSingleton<IInternalToolService, InternalToolService>();
    services.AddSingleton<ChatService>();
    services.AddSingleton<IChatService>(sp => sp.GetRequiredService<ChatService>());
    
    // LLM providers
    services.AddSingleton<OpenAIProvider>();
    services.AddSingleton<ClaudeProvider>();
    services.AddSingleton<OllamaProvider>();
    
    // MCP client
    services.AddSingleton<IMCPClient, MCPClient>();
    
    // Provider factory
    services.AddSingleton<ILLMProviderFactory>(sp =>
    {
        var factory = new LLMProviderFactory(sp);
        factory.RegisterProvider<OpenAIProvider>("openai");
        factory.RegisterProvider<ClaudeProvider>("claude");
        factory.RegisterProvider<OllamaProvider>("ollama");
        return factory;
    });
    
    // Default provider
    services.AddSingleton<ILLMProvider>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var configService = sp.GetRequiredService<IConfigurationService>();
        var providerName = configService.GetValue("DefaultProvider") ?? config["DefaultProvider"] ?? "openai";
        var factory = sp.GetRequiredService<ILLMProviderFactory>();
        
        try
        {
            return factory.GetProvider(providerName);
        }
        catch
        {
            // If the default provider is not available, return a dummy provider
            // The setup command will handle configuration
            return factory.GetProvider("openai");
        }
    });
});

// Check if we need to run setup on first launch
var configService = new ConfigurationService(configuration);
var hasProvider = !string.IsNullOrWhiteSpace(configService.GetValue("DefaultProvider")) ||
                  !string.IsNullOrWhiteSpace(configService.GetValue("OpenAI:ApiKey")) ||
                  !string.IsNullOrWhiteSpace(configService.GetValue("Claude:ApiKey")) ||
                  !string.IsNullOrWhiteSpace(configService.GetValue("Ollama:BaseUrl"));

// Test command for markdown rendering
if (args.Length > 0 && args[0] == "test-markdown")
{
    TestMarkdown.RunTest();
    return 0;
}

// If running with arguments, execute as command
if (args.Length > 0 && !args[0].StartsWith("-"))
{
    // Build the app for command mode
    var app = new CommandApp(builder);
    
    app.Configure(config =>
    {
        config.SetApplicationName("codeagent");
        
        // Register commands
        config.AddCommand<ScanCommand>("scan")
            .WithDescription("Scan project files and show statistics");
        
        config.AddCommand<ProviderCommand>("provider")
            .WithDescription("Manage LLM providers");
        
        config.AddCommand<ModelCommand>("model")
            .WithDescription("Switch LLM models (Ollama only)");
        
        config.AddCommand<PromptCommand>("prompt")
            .WithDescription("Manage system prompt configuration");
        
        config.AddCommand<SetupCommand>("setup")
            .WithDescription("Configure LLM providers");
        
        // File modification commands
        config.AddCommand<EditCommand>("edit")
            .WithDescription("Edit a file with AI assistance");
        
        config.AddCommand<EditMultipleCommand>("edit-multiple")
            .WithDescription("Edit multiple files with AI assistance");
        
        config.AddCommand<DiffCommand>("diff")
            .WithDescription("Show pending changes");
        
        config.AddCommand<ApplyCommand>("apply")
            .WithDescription("Apply pending changes");
        
        config.AddCommand<RejectCommand>("reject")
            .WithDescription("Reject pending changes");
        
        // Analysis and refactoring commands
        config.AddCommand<AnalyzeCommand>("analyze")
            .WithDescription("Analyze code for issues and improvements");
        
        config.AddCommand<RefactorCommand>("refactor")
            .WithDescription("Perform AI-guided refactoring");
        
        config.AddCommand<SearchCommand>("search")
            .WithDescription("Search code with AI context");
    });
    
    return await app.RunAsync(args);
}

// Interactive shell mode
var serviceProvider = builder.Build().Services;

if (!hasProvider)
{
    // Show setup prompt for first-time users
    Console.WriteLine("Welcome to CodeAgent! No LLM provider is configured.");
    Console.WriteLine("Starting setup wizard...");
    Console.WriteLine();
    
    var setupCommand = new SetupCommand(serviceProvider);
    await setupCommand.ExecuteAsync(null!);
}
else
{
    // Validate provider and model on startup
    await ValidateProviderAndModelAsync(serviceProvider);
}

// Run interactive shell
return await builder.UseConsoleLifetime()
    .RunInteractiveShell(
        "CodeAgent",
        historyFile,
        args,
        configurator =>
        {
            // Register commands for the shell
            configurator.AddCommand<ScanCommand>("scan")
                .WithDescription("Scan project files");
            
            configurator.AddCommand<ProviderCommand>("provider")
                .WithDescription("Manage LLM providers");
            
            configurator.AddCommand<ModelCommand>("model")
                .WithDescription("Switch LLM models (Ollama only)");
            
            configurator.AddCommand<PromptCommand>("prompt")
                .WithDescription("Manage system prompt configuration");
            
            configurator.AddCommand<SetupCommand>("setup")
                .WithDescription("Configure LLM providers");
            
            // File modification commands
            configurator.AddCommand<EditCommand>("edit")
                .WithDescription("Edit a file with AI assistance");
            
            configurator.AddCommand<EditMultipleCommand>("edit-multiple")
                .WithDescription("Edit multiple files with AI assistance");
            
            configurator.AddCommand<DiffCommand>("diff")
                .WithDescription("Show pending changes");
            
            configurator.AddCommand<ApplyCommand>("apply")
                .WithDescription("Apply pending changes");
            
            configurator.AddCommand<RejectCommand>("reject")
                .WithDescription("Reject pending changes");
            
            // Analysis and refactoring commands
            configurator.AddCommand<AnalyzeCommand>("analyze")
                .WithDescription("Analyze code for issues and improvements");
            
            configurator.AddCommand<RefactorCommand>("refactor")
                .WithDescription("Perform AI-guided refactoring");
            
            configurator.AddCommand<SearchCommand>("search")
                .WithDescription("Search code with AI context");
        });

static async Task ValidateProviderAndModelAsync(IServiceProvider serviceProvider)
{
    try
    {
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();
        var factory = serviceProvider.GetRequiredService<ILLMProviderFactory>();
        var console = AnsiConsole.Console;
        
        var currentProvider = configService.GetValue("DefaultProvider");
        if (string.IsNullOrWhiteSpace(currentProvider))
        {
            return; // No provider configured, this was handled above
        }
        
        console.MarkupLine($"[dim]Validating {currentProvider} provider...[/]");
        
        var provider = factory.GetProvider(currentProvider);
        if (!provider.IsConfigured)
        {
            console.MarkupLine($"[yellow]Provider '{currentProvider}' is not properly configured.[/]");
            
            if (AnsiConsole.Confirm($"Would you like to reconfigure {currentProvider}?"))
            {
                var setupCommand = new SetupCommand(serviceProvider);
                await setupCommand.ExecuteAsync(null!);
                return;
            }
            else
            {
                console.MarkupLine("[dim]You can run '/setup' later to configure the provider.[/]");
            }
        }
        else
        {
            // Check connection
            var isConnected = await provider.ValidateConnectionAsync();
            if (!isConnected)
            {
                console.MarkupLine($"[red]Cannot connect to {currentProvider}.[/]");
                console.MarkupLine("[dim]Please check your internet connection and API configuration.[/]");
                
                if (AnsiConsole.Confirm($"Would you like to reconfigure {currentProvider}?"))
                {
                    var setupCommand = new SetupCommand(serviceProvider);
                    await setupCommand.ExecuteAsync(null!);
                    return;
                }
                else if (AnsiConsole.Confirm("Would you like to select a different provider?"))
                {
                    var providerCommand = new ProviderCommand(serviceProvider);
                    var context = new CommandContext(Array.Empty<string>(), new EmptyRemaining(), "", null);
                    await providerCommand.ExecuteAsync(context, new ProviderCommand.Settings());
                    return;
                }
            }
            else
            {
                console.MarkupLine($"[green]✓[/] Connected to {currentProvider}");
                
                // For Ollama, also validate the model
                if (currentProvider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                {
                    await ValidateOllamaModelAsync(serviceProvider, console);
                }
            }
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.Console.MarkupLine($"[red]Error during startup validation: {ex.Message}[/]");
    }
}

static async Task ValidateOllamaModelAsync(IServiceProvider serviceProvider, IAnsiConsole console)
{
    try
    {
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();
        var ollamaOptions = serviceProvider.GetRequiredService<IOptions<OllamaOptions>>();
        var currentModel = ollamaOptions.Value.DefaultModel ?? "llama3.2";
        
        console.MarkupLine($"[dim]Checking Ollama model: {currentModel}...[/]");
        
        // Try to get available models to validate current model exists
        var httpClient = serviceProvider.GetService<HttpClient>() ?? new HttpClient();
        var baseUrl = ollamaOptions.Value.BaseUrl?.TrimEnd('/') ?? "http://localhost:11434";
        
        var response = await httpClient.GetAsync($"{baseUrl}/api/tags");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var models = System.Text.Json.JsonSerializer.Deserialize<OllamaModelsResponse>(content);
            var availableModels = models?.Models?.Select(m => m.Name.Split(':')[0]).Distinct().ToList() ?? new List<string>();
            
            if (availableModels.Contains(currentModel))
            {
                console.MarkupLine($"[green]✓[/] Model {currentModel} is available");
            }
            else
            {
                console.MarkupLine($"[yellow]Model '{currentModel}' not found in Ollama.[/]");
                console.MarkupLine($"[dim]Available models: {string.Join(", ", availableModels)}[/]");
                
                if (availableModels.Any() && AnsiConsole.Confirm("Would you like to select an available model?"))
                {
                    var modelCommand = new ModelCommand(serviceProvider);
                    var context = new CommandContext(Array.Empty<string>(), new EmptyRemaining(), "", null);
                    await modelCommand.ExecuteAsync(context, new ModelCommand.Settings());
                }
                else if (AnsiConsole.Confirm($"Would you like to install {currentModel}?"))
                {
                    console.MarkupLine($"[dim]Run: ollama pull {currentModel}[/]");
                }
            }
        }
        else
        {
            console.MarkupLine("[yellow]Could not check available models. Make sure Ollama is running.[/]");
        }
    }
    catch (Exception ex)
    {
        console.MarkupLine($"[yellow]Could not validate Ollama model: {ex.Message}[/]");
    }
}

// Helper classes for Ollama API response
public class OllamaModelsResponse
{
    public List<OllamaModel> Models { get; set; } = new();
}

public class OllamaModel
{
    public string Name { get; set; } = string.Empty;
}

// Helper class for CommandContext
public class EmptyRemaining : Spectre.Console.Cli.IRemainingArguments
{
    public IReadOnlyList<string> Raw => Array.Empty<string>();
    public ILookup<string, string?> Parsed => (new List<(string, string?)>()).ToLookup(x => x.Item1, x => x.Item2);
}

public partial class Program { }
