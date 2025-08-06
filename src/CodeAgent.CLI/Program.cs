using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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

// Build the app
var app = new CommandApp(builder);

app.Configure(config =>
{
    config.SetApplicationName("codeagent");
    
    // Set root command as default
    config.AddCommand<RootCommand>("");
    
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

public partial class Program { }
