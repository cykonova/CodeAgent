using System.Text;
using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.CommandLine.Parsing;
using CodeAgent.CLI.Rendering;

namespace CodeAgent.CLI.Shell;

public class InteractiveShell : IPrompt<int>
{
    private readonly string _prompt;
    private readonly IList<string> _history;
    private readonly ICommandApp _commandApp;
    private readonly IServiceProvider _serviceProvider;
    private readonly ShellSettings _settings;
    private readonly StringBuilder _currentLine;
    private readonly IChatService _chatService;
    private readonly ILLMProvider _llmProvider;
    private readonly MarkdigRenderer _markdownRenderer;
    
    private int _cursorIndex;
    private int _historyIndex = -1;
    private string? _savedLine;

    private static class InternalCommands
    {
        public const string Help = "help";
        public const string Exit = "exit";
        public const string Clear = "clear";
    }

    public InteractiveShell(
        string prompt,
        IList<string> history,
        ICommandApp commandApp,
        IServiceProvider serviceProvider,
        ShellSettings settings)
    {
        _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        _history = history;
        _commandApp = commandApp;
        _serviceProvider = serviceProvider;
        _settings = settings;
        _currentLine = new StringBuilder();
        _cursorIndex = 0;
        
        _chatService = _serviceProvider.GetRequiredService<IChatService>();
        _llmProvider = _serviceProvider.GetRequiredService<ILLMProvider>();
        _markdownRenderer = new MarkdigRenderer();
    }

    public int Show(IAnsiConsole console)
    {
        return ShowAsync(console, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task<int> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(console);

        // Display welcome message
        console.Write(new FigletText("CodeAgent").Centered().Color(Color.Blue));
        console.MarkupLine("[bold blue]AI-Powered Coding Assistant[/]");
        console.WriteLine();
        console.MarkupLine($"[green]Type messages to chat, use [bold]{_settings.CommandPrefix}command[/] for commands, or [bold]exit[/] to quit.[/]");
        console.MarkupLine("[dim]Commands: /help, /scan, /provider, /config, /clear[/]");
        console.WriteLine();

        WritePrompt(console);

        while (true)
        {
            var key = await console.Input.ReadKeyAsync(true, cancellationToken);
            if (key == null) continue;
            
            switch (key.Value.Key)
            {
                case ConsoleKey.Enter:
                    console.WriteLine();
                    if (!await Execute(console)) return 0;
                    Reset(console);
                    break;
                    
                case ConsoleKey.Backspace:
                    BackSpace(console);
                    break;
                    
                case ConsoleKey.LeftArrow:
                    if (_cursorIndex == 0) break;
                    console.Cursor.Move(CursorDirection.Left, 1);
                    _cursorIndex--;
                    break;
                    
                case ConsoleKey.RightArrow:
                    if (_cursorIndex == _currentLine.Length) break;
                    console.Cursor.Move(CursorDirection.Right, 1);
                    _cursorIndex++;
                    break;
                    
                case ConsoleKey.UpArrow:
                    PreviousHistory(console);
                    break;
                    
                case ConsoleKey.DownArrow:
                    NextHistory(console);
                    break;
                    
                case ConsoleKey.Tab:
                    // Tab completion for commands
                    if (_currentLine.Length > 0 && _currentLine[0] == '/')
                    {
                        AutoCompleteCommand(console);
                    }
                    break;
                    
                default:
                    if (key.Value.Modifiers != ConsoleModifiers.None) continue;
                    Insert(console, key.Value.KeyChar);
                    break;
            }
        }
    }

    private void WritePrompt(IAnsiConsole console)
    {
        console.Markup($"[bold cyan]{_prompt}[/]");
        _cursorIndex = 0;
    }

    private async Task<bool> Execute(IAnsiConsole console)
    {
        var input = _currentLine.ToString().Trim();
        
        // Return to latest history
        _historyIndex = -1;

        // Handle special cases
        if (string.IsNullOrWhiteSpace(input)) return true;
        if (input.Equals(InternalCommands.Exit, StringComparison.OrdinalIgnoreCase)) return false;
        
        if (input.Equals(InternalCommands.Clear, StringComparison.OrdinalIgnoreCase))
        {
            console.Clear();
            _chatService.ClearHistory();
            console.MarkupLine("[green]Screen and chat history cleared.[/]");
            return true;
        }

        // Add to history
        if (_history.Count == 0 || !_history[0].Equals(input, StringComparison.OrdinalIgnoreCase))
        {
            _history.Insert(0, input);
        }

        // Check if it's a command or chat
        if (input.StartsWith(_settings.CommandPrefix))
        {
            // It's a command - remove the prefix and execute
            var commandLine = input.Substring(_settings.CommandPrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                commandLine = "help";
            }
            
            var args = ProcessCommandLine(commandLine);
            try
            {
                await _commandApp.RunAsync(args);
            }
            catch (Exception ex)
            {
                console.MarkupLine($"[red]Command error: {ex.Message}[/]");
            }
        }
        else
        {
            // It's a chat message
            await ProcessChatMessage(console, input);
        }

        return true;
    }

    private async Task ProcessChatMessage(IAnsiConsole console, string message)
    {
        if (!_llmProvider.IsConfigured)
        {
            console.MarkupLine("[red]LLM provider is not configured. Run [bold]/setup[/] to configure.[/]");
            return;
        }

        console.MarkupLine("[bold green]Assistant:[/]");
        
        await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Thinking...", async ctx =>
            {
                try
                {
                    var response = await _chatService.ProcessMessageAsync(message);
                    
                    ctx.Status("Generating response...");
                    
                    if (!string.IsNullOrEmpty(response.Error))
                    {
                        console.MarkupLine($"[red]Error: {response.Error}[/]");
                    }
                    else
                    {
                        // Render markdown response
                        _markdownRenderer.Render(console, response.Content);
                    }
                }
                catch (Exception ex)
                {
                    console.MarkupLine($"[red]Error: {ex.Message}[/]");
                }
            });
        
        console.WriteLine();
    }

    private IEnumerable<string> ProcessCommandLine(string line)
    {
        return CommandLineStringSplitter.Instance
            .Split(line.Trim())
            .Select(word => word.ToLower().Equals(InternalCommands.Help) ? "-h" : word);
    }

    private void PreviousHistory(IAnsiConsole console)
    {
        if (_historyIndex == _history.Count - 1) return;
        if (_historyIndex == -1)
        {
            _savedLine = _currentLine.ToString();
        }

        _historyIndex++;
        Reset(console, _history[_historyIndex]);
    }

    private void NextHistory(IAnsiConsole console)
    {
        if (_historyIndex == -1) return;

        _historyIndex--;
        var lineToDisplay = _historyIndex == -1
            ? _savedLine
            : _history[_historyIndex];

        Reset(console, lineToDisplay);
    }

    private void BackSpace(IAnsiConsole console)
    {
        if (_cursorIndex == 0) return;

        _cursorIndex--;
        _currentLine.Remove(_cursorIndex, 1);
        
        // Simple backspace handling - rewrite from cursor position
        console.Cursor.Move(CursorDirection.Left, 1);
        console.Write(new string(' ', _currentLine.Length - _cursorIndex + 1));
        console.Cursor.Move(CursorDirection.Left, _currentLine.Length - _cursorIndex + 1);
        
        // Rewrite the rest of the line
        if (_cursorIndex < _currentLine.Length)
        {
            console.Write(_currentLine.ToString(_cursorIndex, _currentLine.Length - _cursorIndex));
            console.Cursor.Move(CursorDirection.Left, _currentLine.Length - _cursorIndex);
        }
    }

    private void Insert(IAnsiConsole console, char input)
    {
        _currentLine.Insert(_cursorIndex, input);
        
        // Write from current position to end
        var textToWrite = _currentLine.ToString(_cursorIndex, _currentLine.Length - _cursorIndex);
        console.Write(textToWrite);
        
        // Move cursor to correct position
        _cursorIndex++;
        if (_cursorIndex < _currentLine.Length)
        {
            console.Cursor.Move(CursorDirection.Left, _currentLine.Length - _cursorIndex);
        }
    }

    private void Reset(IAnsiConsole console, string? line = default)
    {
        // Clear current line
        console.Cursor.Move(CursorDirection.Left, _cursorIndex);
        console.Write(new string(' ', _currentLine.Length));
        console.Cursor.Move(CursorDirection.Left, _currentLine.Length);
        
        _currentLine.Clear();
        WritePrompt(console);

        if (!string.IsNullOrWhiteSpace(line))
        {
            _currentLine.Append(line);
            console.Write(_currentLine.ToString());
        }

        _cursorIndex = _currentLine.Length;
    }

    private void AutoCompleteCommand(IAnsiConsole console)
    {
        var currentCommand = _currentLine.ToString().Substring(1); // Remove the '/' prefix
        var availableCommands = new[] { "help", "scan", "provider", "config", "setup", "mcp", "clear", "exit" };
        
        var matches = availableCommands.Where(c => c.StartsWith(currentCommand, StringComparison.OrdinalIgnoreCase)).ToList();
        
        if (matches.Count == 1)
        {
            // Complete the command
            var completion = matches[0].Substring(currentCommand.Length);
            Insert(console, completion.ToCharArray()[0]);
            for (int i = 1; i < completion.Length; i++)
            {
                Insert(console, completion[i]);
            }
        }
        else if (matches.Count > 1)
        {
            // Show available options
            console.WriteLine();
            console.MarkupLine($"[dim]Available commands: {string.Join(", ", matches)}[/]");
            WritePrompt(console);
            console.Write(_currentLine.ToString());
            _cursorIndex = _currentLine.Length;
        }
    }
}