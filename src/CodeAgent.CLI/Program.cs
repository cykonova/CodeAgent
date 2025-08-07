using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CodeAgent.CLI;
using CodeAgent.CLI.Commands;
using CodeAgent.CLI.Services;
using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Infrastructure.Services;
using CodeAgent.Providers.OpenAI;
using CodeAgent.Providers.Claude;
using CodeAgent.Providers.Ollama;
using CodeAgent.MCP;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http;
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

// Create service collection and register services
var services = new ServiceCollection();

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
services.AddHttpClient("OllamaClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
services.AddHttpClient();

// Console
services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
services.AddSingleton<IPermissionPrompt, ConsolePermissionPrompt>();
services.AddSingleton<RootCommand>();

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
services.AddSingleton<OllamaProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("OllamaClient");
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>();
    var logger = sp.GetRequiredService<ILogger<OllamaProvider>>();
    return new OllamaProvider(options, logger, httpClient);
});

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

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Create the app with dependency injection
var registrar = new TypeRegistrar(serviceProvider);
var app = new CommandApp(registrar);

// Configure the app
app.SetDefaultCommand<RootCommand>();
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

// If no arguments provided, launch web portal
if (args.Length == 0)
{
    // Launch the web portal in daemon mode
    var rootCommand = serviceProvider.GetRequiredService<RootCommand>();
    var remaining = new DummyRemainingArguments();
    return await rootCommand.ExecuteAsync(new CommandContext(new List<string>(), remaining, "root", null));
}

// Otherwise, execute the requested command
return await app.RunAsync(args);

public partial class Program { }

// Dummy implementation for IRemainingArguments
public class DummyRemainingArguments : Spectre.Console.Cli.IRemainingArguments
{
    public ILookup<string, string?> Parsed => new List<(string, string?)>().ToLookup(x => x.Item1, x => x.Item2);
    public IReadOnlyList<string> Raw => new List<string>();
}
