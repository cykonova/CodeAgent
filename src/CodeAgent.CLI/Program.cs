using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using CodeAgent.CLI.Commands;
using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Infrastructure.Services;
using CodeAgent.Providers.OpenAI;
using CodeAgent.Providers.Claude;
using CodeAgent.Providers.Ollama;
using CodeAgent.MCP;
using Microsoft.Extensions.Configuration;

// Build configuration with user settings from ~/.codeagent/settings.json
var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var userSettingsPath = Path.Combine(userHome, ".codeagent", "settings.json");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(userSettingsPath, optional: true, reloadOnChange: true) // Load user settings
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var services = new ServiceCollection();

// Register configuration
services.AddSingleton<IConfiguration>(configuration);
services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
services.Configure<ClaudeOptions>(configuration.GetSection("Claude"));
services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
services.Configure<MCPOptions>(configuration.GetSection("MCP"));

// Register logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

// Register HttpClient
services.AddHttpClient();

// Register core services
services.AddSingleton<IConfigurationService, ConfigurationService>();
services.AddSingleton<IFileSystemService, FileSystemService>();
services.AddSingleton<IChatService, ChatService>();

// Register LLM providers
services.AddSingleton<OpenAIProvider>();
services.AddSingleton<ClaudeProvider>();
services.AddSingleton<OllamaProvider>();

// Register MCP client
services.AddSingleton<IMCPClient, MCPClient>();

// Register provider factory
services.AddSingleton<ILLMProviderFactory>(sp =>
{
    var factory = new LLMProviderFactory(sp);
    factory.RegisterProvider<OpenAIProvider>("openai");
    factory.RegisterProvider<ClaudeProvider>("claude");
    factory.RegisterProvider<OllamaProvider>("ollama");
    return factory;
});

// Register default provider based on configuration
services.AddSingleton<ILLMProvider>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var providerName = config["DefaultProvider"] ?? "openai";
    var factory = sp.GetRequiredService<ILLMProviderFactory>();
    return factory.GetProvider(providerName);
});

var serviceProvider = services.BuildServiceProvider();

// Check if this is first run (no provider configured)
var configService = serviceProvider.GetRequiredService<IConfigurationService>();
var defaultProvider = configService.GetValue("DefaultProvider");
var cmdArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

// Check if any provider is configured (either in settings or environment)
var hasConfiguredProvider = !string.IsNullOrWhiteSpace(defaultProvider);

// Also check if any provider has API keys configured
if (!hasConfiguredProvider)
{
    var openAIKey = configService.GetValue("OpenAI:ApiKey");
    var claudeKey = configService.GetValue("Claude:ApiKey");
    var ollamaUrl = configService.GetValue("Ollama:BaseUrl");
    
    hasConfiguredProvider = !string.IsNullOrWhiteSpace(openAIKey) || 
                           !string.IsNullOrWhiteSpace(claudeKey) || 
                           !string.IsNullOrWhiteSpace(ollamaUrl);
}

// If no provider is configured and not running setup command, prompt for setup
if (!hasConfiguredProvider && 
    (cmdArgs.Length == 0 || cmdArgs[0].ToLower() != "setup"))
{
    AnsiConsole.MarkupLine("[yellow]No LLM provider configured. Starting setup wizard...[/]");
    AnsiConsole.WriteLine();
    
    var setupCommand = new SetupCommand(serviceProvider);
    await setupCommand.ExecuteAsync();
    
    // Rebuild service provider to pick up new configuration
    configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile(userSettingsPath, optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddUserSecrets<Program>(optional: true)
        .Build();
    
    services = new ServiceCollection();
    
    // Re-register all services with new configuration
    services.AddSingleton<IConfiguration>(configuration);
    services.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
    services.Configure<ClaudeOptions>(configuration.GetSection("Claude"));
    services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
    services.Configure<MCPOptions>(configuration.GetSection("MCP"));
    
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Warning);
    });
    
    services.AddHttpClient();
    services.AddSingleton<IConfigurationService, ConfigurationService>();
    services.AddSingleton<IFileSystemService, FileSystemService>();
    services.AddSingleton<IChatService, ChatService>();
    services.AddSingleton<OpenAIProvider>();
    services.AddSingleton<ClaudeProvider>();
    services.AddSingleton<OllamaProvider>();
    services.AddSingleton<IMCPClient, MCPClient>();
    
    services.AddSingleton<ILLMProviderFactory>(sp =>
    {
        var factory = new LLMProviderFactory(sp);
        factory.RegisterProvider<OpenAIProvider>("openai");
        factory.RegisterProvider<ClaudeProvider>("claude");
        factory.RegisterProvider<OllamaProvider>("ollama");
        return factory;
    });
    
    services.AddSingleton<ILLMProvider>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var providerName = config["DefaultProvider"] ?? "openai";
        var factory = sp.GetRequiredService<ILLMProviderFactory>();
        return factory.GetProvider(providerName);
    });
    
    serviceProvider = services.BuildServiceProvider();
}

// Command line arguments already parsed above

// Display welcome message
AnsiConsole.Write(
    new FigletText("CodeAgent")
        .Centered()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[bold blue]Welcome to CodeAgent - Your AI Coding Assistant[/]");
AnsiConsole.WriteLine();

// Handle commands
if (cmdArgs.Length > 0)
{
    var command = cmdArgs[0].ToLower();
    
    switch (command)
    {
        case "setup":
            var setupCommand = new SetupCommand(serviceProvider);
            await setupCommand.ExecuteAsync();
            break;
            
        case "provider":
            if (cmdArgs.Length > 1 && cmdArgs[1].ToLower() == "select" && cmdArgs.Length > 2)
            {
                var providerName = cmdArgs[2].ToLower();
                var factory = serviceProvider.GetRequiredService<ILLMProviderFactory>();
                
                if (factory.GetAvailableProviders().Contains(providerName))
                {
                    // Update configuration
                    await configService.SetValueAsync("DefaultProvider", providerName);
                    AnsiConsole.MarkupLine($"[green]Provider set to: {providerName}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Unknown provider: {providerName}[/]");
                    AnsiConsole.MarkupLine($"[yellow]Available providers: {string.Join(", ", factory.GetAvailableProviders())}[/]");
                }
            }
            else if (cmdArgs.Length > 1 && cmdArgs[1].ToLower() == "list")
            {
                var factory = serviceProvider.GetRequiredService<ILLMProviderFactory>();
                AnsiConsole.MarkupLine("[bold]Available providers:[/]");
                foreach (var provider in factory.GetAvailableProviders())
                {
                    AnsiConsole.MarkupLine($"  • {provider}");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Usage: codeagent provider [select|list] [provider-name][/]");
            }
            break;
            
        case "config":
            if (cmdArgs.Length > 2 && cmdArgs[1].ToLower() == "set")
            {
                var key = cmdArgs[2];
                var value = cmdArgs.Length > 3 ? cmdArgs[3] : string.Empty;
                
                await configService.SetValueAsync(key, value);
                AnsiConsole.MarkupLine($"[green]Configuration set: {key}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Usage: codeagent config set <key> <value>[/]");
            }
            break;
            
        case "mcp":
            if (cmdArgs.Length > 2 && cmdArgs[1].ToLower() == "connect")
            {
                var serverUrl = cmdArgs[2];
                var mcpClient = serviceProvider.GetRequiredService<IMCPClient>();
                
                var connected = await mcpClient.ConnectAsync(serverUrl);
                if (connected)
                {
                    AnsiConsole.MarkupLine($"[green]Connected to MCP server at {serverUrl}[/]");
                    
                    var tools = await mcpClient.GetAvailableToolsAsync();
                    AnsiConsole.MarkupLine($"[blue]Available tools: {tools.Count()}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to connect to MCP server at {serverUrl}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Usage: codeagent mcp connect <server-url>[/]");
            }
            break;
            
        case "init":
            AnsiConsole.MarkupLine("[yellow]Project initialization - scanning directory...[/]");
            var fileSystem = serviceProvider.GetRequiredService<IFileSystemService>();
            var currentDir = Directory.GetCurrentDirectory();
            var files = await fileSystem.GetProjectFilesAsync(currentDir);
            AnsiConsole.MarkupLine($"[green]Found {files.Count()} files in project[/]");
            break;
            
        case "scan":
            AnsiConsole.MarkupLine("[yellow]Scanning project structure...[/]");
            var scanner = serviceProvider.GetRequiredService<IFileSystemService>();
            var projectFiles = await scanner.GetProjectFilesAsync(Directory.GetCurrentDirectory());
            
            var table = new Table();
            table.AddColumn("File Type");
            table.AddColumn("Count");
            
            var fileGroups = projectFiles.GroupBy(f => Path.GetExtension(f) ?? "no extension");
            foreach (var group in fileGroups.OrderBy(g => g.Key))
            {
                table.AddRow(group.Key, group.Count().ToString());
            }
            
            AnsiConsole.Write(table);
            break;
            
        case "ask":
            if (cmdArgs.Length > 1)
            {
                var question = string.Join(" ", cmdArgs.Skip(1));
                var chatService = serviceProvider.GetRequiredService<IChatService>();
                
                AnsiConsole.MarkupLine("[yellow]Processing your question...[/]");
                var response = await chatService.ProcessMessageAsync(question);
                
                if (response.IsComplete)
                {
                    AnsiConsole.MarkupLine($"[green]{response.Content}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Error: {response.Error}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Usage: codeagent ask <question>[/]");
            }
            break;
            
        default:
            AnsiConsole.MarkupLine($"[red]Unknown command: {command}[/]");
            AnsiConsole.MarkupLine("[yellow]Available commands: setup, provider, config, mcp, init, scan, ask[/]");
            break;
    }
}
else
{
    // Interactive chat mode
    var chatCommand = new ChatCommand(serviceProvider);
    await chatCommand.ExecuteAsync();
}

public partial class Program { }