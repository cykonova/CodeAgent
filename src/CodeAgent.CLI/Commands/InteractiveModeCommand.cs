using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text;

namespace CodeAgent.CLI.Commands;

[Description("Enhanced interactive mode with context awareness and multi-line editing")]
public class InteractiveModeCommand : AsyncCommand<InteractiveModeCommand.Settings>
{
    private readonly ILLMProvider _llmProvider;
    private readonly IContextManager _contextManager;
    private readonly ISessionService _sessionService;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystemService _fileSystem;
    
    private Session? _currentSession;
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;

    public class Settings : CommandSettings
    {
        [CommandOption("-c|--context")]
        [Description("Load project context from directory")]
        public string? ContextPath { get; set; }

        [CommandOption("-s|--session")]
        [Description("Resume a previous session")]
        public string? SessionId { get; set; }

        [CommandOption("--provider")]
        [Description("Specify the LLM provider to use")]
        public string? Provider { get; set; }

        [CommandOption("--multiline")]
        [Description("Enable multi-line input mode")]
        public bool MultiLine { get; set; }
    }

    public InteractiveModeCommand(
        ILLMProvider llmProvider,
        IContextManager contextManager,
        ISessionService sessionService,
        IProviderManager providerManager,
        IFileSystemService fileSystem)
    {
        _llmProvider = llmProvider;
        _contextManager = contextManager;
        _sessionService = sessionService;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Initialize session
        await InitializeSessionAsync(settings);
        
        // Load context if specified
        if (!string.IsNullOrEmpty(settings.ContextPath))
        {
            await LoadContextAsync(settings.ContextPath);
        }

        // Switch provider if specified
        if (!string.IsNullOrEmpty(settings.Provider))
        {
            await _providerManager.SwitchProviderAsync(settings.Provider);
        }

        DisplayWelcomeMessage();
        DisplayHelp();

        // Main interaction loop
        while (true)
        {
            try
            {
                var input = settings.MultiLine 
                    ? await GetMultiLineInputAsync() 
                    : await GetSingleLineInputAsync();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                // Check for commands
                if (input.StartsWith('/'))
                {
                    var shouldContinue = await HandleCommandAsync(input);
                    if (!shouldContinue)
                        break;
                    continue;
                }

                // Add to history
                _commandHistory.Add(input);
                _historyIndex = _commandHistory.Count;

                // Process with LLM
                await ProcessInputAsync(input);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }
        }

        // Save session before exit
        await SaveSessionAsync();
        
        return 0;
    }

    private async Task InitializeSessionAsync(Settings settings)
    {
        if (!string.IsNullOrEmpty(settings.SessionId))
        {
            _currentSession = await _sessionService.LoadSessionAsync(settings.SessionId);
            if (_currentSession != null)
            {
                AnsiConsole.MarkupLine($"[green]Resumed session: {_currentSession.Name}[/]");
                await DisplaySessionHistoryAsync();
            }
        }

        if (_currentSession == null)
        {
            _currentSession = await _sessionService.CreateSessionAsync($"Interactive-{DateTime.Now:yyyy-MM-dd-HH-mm}");
        }
    }

    private async Task LoadContextAsync(string path)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Loading context from [cyan]{path}[/]...", async ctx =>
            {
                var context = await _contextManager.BuildContextAsync(path);
                
                AnsiConsole.MarkupLine($"[green]✓[/] Loaded context for [cyan]{context.ProjectName}[/]");
                AnsiConsole.MarkupLine($"  Project type: {context.ProjectType}");
                AnsiConsole.MarkupLine($"  Primary language: {context.PrimaryLanguage ?? "Unknown"}");
                AnsiConsole.MarkupLine($"  Source files: {context.SourceFiles.Count}");
                AnsiConsole.MarkupLine($"  Test files: {context.TestFiles.Count}");
                
                if (context.Dependencies.Any())
                {
                    AnsiConsole.MarkupLine($"  Dependencies: {context.Dependencies.Count}");
                }
            });
    }

    private void DisplayWelcomeMessage()
    {
        var panel = new Panel(new FigletText("CodeAgent")
            .Centered()
            .Color(Color.Aqua))
            .Header("[yellow]Enhanced Interactive Mode[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue);
        
        AnsiConsole.Write(panel);
        
        var provider = _providerManager.CurrentProviderName ?? "Default";
        AnsiConsole.MarkupLine($"[dim]Provider: {provider} | Session: {_currentSession?.Name}[/]");
    }

    private void DisplayHelp()
    {
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn("")
            .AddColumn("");

        table.AddRow("[cyan]/help[/]", "Show this help message");
        table.AddRow("[cyan]/exit[/]", "Exit interactive mode");
        table.AddRow("[cyan]/clear[/]", "Clear the screen");
        table.AddRow("[cyan]/history[/]", "Show command history");
        table.AddRow("[cyan]/context <path>[/]", "Load project context");
        table.AddRow("[cyan]/provider <name>[/]", "Switch LLM provider");
        table.AddRow("[cyan]/save[/]", "Save current session");
        table.AddRow("[cyan]/sessions[/]", "List available sessions");
        table.AddRow("[cyan]/files[/]", "Show relevant files");
        table.AddRow("[cyan]/multiline[/]", "Toggle multi-line mode");
        table.AddRow("[cyan]/undo[/]", "Undo last file change");
        table.AddRow("[cyan]/redo[/]", "Redo file change");
        
        AnsiConsole.MarkupLine("\n[yellow]Commands:[/]");
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[dim]Type your message or use a command. Press Ctrl+C to exit.[/]\n");
    }

    private async Task<string> GetSingleLineInputAsync()
    {
        var prompt = new TextPrompt<string>("[green]>[/]")
            .AllowEmpty()
            .PromptStyle(new Style(foreground: Color.Green));
        
        return await Task.FromResult(AnsiConsole.Prompt(prompt));
    }

    private async Task<string> GetMultiLineInputAsync()
    {
        AnsiConsole.MarkupLine("[dim]Enter your message (press Ctrl+D or type '/end' on a new line to send):[/]");
        var lines = new List<string>();
        
        while (true)
        {
            var line = Console.ReadLine();
            if (line == null || line == "/end")
                break;
            lines.Add(line);
        }
        
        return await Task.FromResult(string.Join(Environment.NewLine, lines));
    }

    private async Task<bool> HandleCommandAsync(string command)
    {
        var parts = command.Split(' ', 2);
        var cmd = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1] : "";

        switch (cmd)
        {
            case "/exit":
            case "/quit":
                AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                return false;

            case "/help":
            case "/?":
                DisplayHelp();
                break;

            case "/clear":
            case "/cls":
                AnsiConsole.Clear();
                DisplayWelcomeMessage();
                break;

            case "/history":
                DisplayHistory();
                break;

            case "/context":
                if (!string.IsNullOrEmpty(args))
                {
                    await LoadContextAsync(args);
                }
                else
                {
                    DisplayCurrentContext();
                }
                break;

            case "/provider":
                if (!string.IsNullOrEmpty(args))
                {
                    await SwitchProviderAsync(args);
                }
                else
                {
                    DisplayProviders();
                }
                break;

            case "/save":
                await SaveSessionAsync();
                break;

            case "/sessions":
                await DisplaySessionsAsync();
                break;

            case "/files":
                DisplayRelevantFiles();
                break;

            case "/multiline":
                AnsiConsole.MarkupLine("[yellow]Multi-line mode is not toggleable in this context[/]");
                break;

            default:
                AnsiConsole.MarkupLine($"[red]Unknown command: {cmd}[/]");
                break;
        }

        return true;
    }

    private async Task ProcessInputAsync(string input)
    {
        // Add to session
        _currentSession?.Messages.Add(new SessionMessage
        {
            Role = "user",
            Content = input,
            Timestamp = DateTime.UtcNow
        });

        // Get relevant context files
        var relevantFiles = new List<string>();
        var currentContext = _contextManager.GetCurrentContext();
        if (currentContext != null)
        {
            relevantFiles = (await _contextManager.GetRelevantFilesAsync(input, 5)).ToList();
            
            if (relevantFiles.Any())
            {
                AnsiConsole.MarkupLine("[dim]Using context from: " + 
                    string.Join(", ", relevantFiles.Select(Path.GetFileName)) + "[/]");
            }
        }

        // Build enhanced prompt with context
        var enhancedPrompt = await BuildEnhancedPromptAsync(input, relevantFiles);

        // Process with LLM
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[cyan]Thinking...[/]", async ctx =>
            {
                var provider = _providerManager.CurrentProvider ?? _llmProvider;
                var request = new ChatRequest
                {
                    Messages = new List<ChatMessage>
                    {
                        new() { Role = "user", Content = enhancedPrompt }
                    },
                    Temperature = 0.7,
                    MaxTokens = 2000
                };

                // Stream response
                AnsiConsole.WriteLine();
                var responseBuilder = new StringBuilder();
                
                await foreach (var chunk in provider.StreamMessageAsync(request))
                {
                    AnsiConsole.Write(chunk);
                    responseBuilder.Append(chunk);
                }
                
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();

                // Add response to session
                _currentSession?.Messages.Add(new SessionMessage
                {
                    Role = "assistant",
                    Content = responseBuilder.ToString(),
                    Timestamp = DateTime.UtcNow
                });
            });
    }

    private async Task<string> BuildEnhancedPromptAsync(string input, List<string> relevantFiles)
    {
        var promptBuilder = new StringBuilder();
        
        // Add context if available
        if (relevantFiles.Any())
        {
            promptBuilder.AppendLine("Context files:");
            foreach (var file in relevantFiles)
            {
                var content = await _fileSystem.ReadFileAsync(file);
                var preview = content.Length > 500 
                    ? content.Substring(0, 500) + "..." 
                    : content;
                
                promptBuilder.AppendLine($"\n--- {Path.GetFileName(file)} ---");
                promptBuilder.AppendLine(preview);
            }
            promptBuilder.AppendLine("\n--- End Context ---\n");
        }
        
        promptBuilder.AppendLine($"User: {input}");
        
        return promptBuilder.ToString();
    }

    private void DisplayHistory()
    {
        if (!_commandHistory.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No command history[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("#")
            .AddColumn("Command");

        for (int i = 0; i < _commandHistory.Count; i++)
        {
            table.AddRow($"{i + 1}", _commandHistory[i].Length > 50 
                ? _commandHistory[i].Substring(0, 50) + "..." 
                : _commandHistory[i]);
        }

        AnsiConsole.Write(table);
    }

    private void DisplayCurrentContext()
    {
        var context = _contextManager.GetCurrentContext();
        if (context == null)
        {
            AnsiConsole.MarkupLine("[yellow]No context loaded[/]");
            return;
        }

        var panel = new Panel($"""
            Project: {context.ProjectName}
            Type: {context.ProjectType}
            Language: {context.PrimaryLanguage ?? "Unknown"}
            Files: {context.SourceFiles.Count} source, {context.TestFiles.Count} tests
            Dependencies: {context.Dependencies.Count}
            Last Updated: {context.LastUpdated:yyyy-MM-dd HH:mm:ss}
            """)
            .Header("[cyan]Current Context[/]")
            .Border(BoxBorder.Rounded);
        
        AnsiConsole.Write(panel);
    }

    private async Task SwitchProviderAsync(string providerName)
    {
        var success = await _providerManager.SwitchProviderAsync(providerName);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Switched to provider: [cyan]{providerName}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Failed to switch to provider: [cyan]{providerName}[/]");
        }
    }

    private void DisplayProviders()
    {
        var providers = _providerManager.GetAvailableProviders();
        var current = _providerManager.CurrentProviderName;
        
        AnsiConsole.MarkupLine("[cyan]Available providers:[/]");
        foreach (var provider in providers)
        {
            var marker = provider == current ? "[green]●[/]" : "[gray]○[/]";
            AnsiConsole.MarkupLine($"  {marker} {provider}");
        }
    }

    private async Task SaveSessionAsync()
    {
        if (_currentSession != null)
        {
            await _sessionService.SaveSessionAsync(_currentSession);
            AnsiConsole.MarkupLine($"[green]✓[/] Session saved: [cyan]{_currentSession.Name}[/]");
        }
    }

    private async Task DisplaySessionsAsync()
    {
        var sessions = await _sessionService.ListSessionsAsync();
        
        if (!sessions.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No sessions available[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("ID")
            .AddColumn("Name")
            .AddColumn("Messages")
            .AddColumn("Created");

        foreach (var session in sessions.Take(10))
        {
            var messages = await _sessionService.GetMessagesAsync(session.Id);
            table.AddRow(
                session.Id.Substring(0, 8),
                session.Name,
                messages?.Count().ToString() ?? "0",
                session.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            );
        }

        AnsiConsole.Write(table);
    }

    private Task DisplaySessionHistoryAsync()
    {
        if (_currentSession == null || !_currentSession.Messages.Any())
            return Task.CompletedTask;

        AnsiConsole.MarkupLine("\n[dim]--- Previous messages ---[/]");
        
        foreach (var message in _currentSession.Messages.TakeLast(5))
        {
            var role = message.Role == "user" ? "[green]You[/]" : "[cyan]Assistant[/]";
            var preview = message.Content.Length > 100 
                ? message.Content.Substring(0, 100) + "..." 
                : message.Content;
            
            AnsiConsole.MarkupLine($"{role}: {preview}");
        }
        
        AnsiConsole.MarkupLine("[dim]--- End of history ---[/]\n");
        
        return Task.CompletedTask;
    }

    private void DisplayRelevantFiles()
    {
        var context = _contextManager.GetCurrentContext();
        if (context == null || !context.SourceFiles.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No files in context[/]");
            return;
        }

        var files = context.SourceFiles.Take(10).ToList();
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("File")
            .AddColumn("Size")
            .AddColumn("Modified");

        foreach (var file in files)
        {
            if (context.FileMetadata.TryGetValue(file, out var metadata))
            {
                table.AddRow(
                    Path.GetFileName(file),
                    $"{metadata.FileSize / 1024.0:F1} KB",
                    metadata.LastModified.ToString("yyyy-MM-dd HH:mm")
                );
            }
        }

        AnsiConsole.Write(table);
    }
}