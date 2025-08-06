using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using CodeAgent.CLI.Commands;
using CodeAgent.Core.Services;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Infrastructure.Services;
using CodeAgent.Providers.OpenAI;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

// Register services
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<IConfigurationService, ConfigurationService>();
services.AddSingleton<IFileSystemService, FileSystemService>();
services.AddSingleton<ILLMProvider, OpenAIProvider>();
services.AddSingleton<IChatService, ChatService>();

var serviceProvider = services.BuildServiceProvider();

// Display welcome message
AnsiConsole.Write(
    new FigletText("CodeAgent")
        .Centered()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[bold blue]Welcome to CodeAgent - Your AI Coding Assistant[/]");
AnsiConsole.WriteLine();

// Main menu loop
var chatCommand = new ChatCommand(serviceProvider);
await chatCommand.ExecuteAsync();

public partial class Program { }