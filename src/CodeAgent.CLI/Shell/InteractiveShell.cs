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
    private readonly IServiceProvider _serviceProvider;
    private readonly ShellSettings _settings;
    private readonly StringBuilder _currentLine;
    private readonly IChatService _chatService;
    private readonly ILLMProvider _llmProvider;
    private readonly MarkdigRenderer _markdownRenderer;
    private readonly ICommandApp _commandApp;
    
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
        
        // Set up project directory for permissions
        var permissionService = _serviceProvider.GetRequiredService<IPermissionService>();
        permissionService.SetProjectDirectory(Environment.CurrentDirectory);
    }

    public int Show(IAnsiConsole console)
    {
        return ShowAsync(console, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task<int> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(console);

        // Check if input is being piped
        if (Console.IsInputRedirected)
        {
            return await ProcessPipedInput(console);
        }

        // Display welcome message
        console.Write(new FigletText("CodeAgent").Centered().Color(Color.Blue));
        console.MarkupLine("[bold blue]AI-Powered Coding Assistant[/]");
        console.WriteLine();
        console.MarkupLine($"[green]Type messages to chat, use [bold]{_settings.CommandPrefix}command[/] for commands, or [bold]{_settings.CommandPrefix}exit[/] to quit.[/]");
        console.MarkupLine("[dim]Commands: /help, /scan, /provider, /model, /prompt, /config, /clear, /exit[/]");
        console.WriteLine();

        WritePrompt(console);

        while (true)
        {
            var key = await console.Input.ReadKeyAsync(true, cancellationToken);
            if (key == null) continue;
            
            // Handle macOS shortcuts
            if (key.Value.Modifiers == ConsoleModifiers.Control)
            {
                switch (key.Value.Key)
                {
                    case ConsoleKey.A: // Cmd+A equivalent (move to beginning of line)
                        MoveCursorToStart(console);
                        continue;
                    case ConsoleKey.E: // Cmd+E equivalent (move to end of line)
                        MoveCursorToEnd(console);
                        continue;
                    case ConsoleKey.U: // Ctrl+U (clear line)
                        ClearLine(console);
                        continue;
                    case ConsoleKey.W: // Ctrl+W (delete word backwards)
                        DeleteWordBackwards(console);
                        continue;
                    case ConsoleKey.K: // Ctrl+K (delete to end of line)
                        DeleteToEndOfLine(console);
                        continue;
                }
            }
            
            // Handle Alt/Option shortcuts (common on macOS)
            if (key.Value.Modifiers == ConsoleModifiers.Alt)
            {
                switch (key.Value.Key)
                {
                    case ConsoleKey.LeftArrow: // Option+Left (move word left)
                        MoveWordLeft(console);
                        continue;
                    case ConsoleKey.RightArrow: // Option+Right (move word right)
                        MoveWordRight(console);
                        continue;
                    case ConsoleKey.Backspace: // Option+Backspace (delete word backwards)
                        DeleteWordBackwards(console);
                        continue;
                }
            }
            
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
                    
                case ConsoleKey.Home:
                    MoveCursorToStart(console);
                    break;
                    
                case ConsoleKey.End:
                    MoveCursorToEnd(console);
                    break;
                    
                case ConsoleKey.Delete:
                    DeleteCharacter(console);
                    break;
                    
                case ConsoleKey.Tab:
                    // Tab completion for commands
                    if (_currentLine.Length > 0 && _currentLine[0] == '/')
                    {
                        AutoCompleteCommand(console);
                    }
                    break;
                    
                default:
                    // Allow printable characters, including those with Shift modifier for uppercase
                    if (key.Value.Modifiers != ConsoleModifiers.None && key.Value.Modifiers != ConsoleModifiers.Shift) continue;
                    if (!char.IsControl(key.Value.KeyChar))
                    {
                        Insert(console, key.Value.KeyChar);
                    }
                    break;
            }
        }
    }

    private void WritePrompt(IAnsiConsole console)
    {
        var promptText = BuildDynamicPrompt();
        // Escape the prompt text to prevent color parsing issues
        var escapedPrompt = promptText.Replace("[", "[[").Replace("]", "]]");
        console.Markup($"[bold cyan]{escapedPrompt}[/]");
        _cursorIndex = 0;
    }

    private string BuildDynamicPrompt()
    {
        try
        {
            var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
            var currentProvider = configService.GetValue("DefaultProvider");
            
            if (string.IsNullOrWhiteSpace(currentProvider))
            {
                return _prompt;
            }
            
            var promptBuilder = new System.Text.StringBuilder("CodeAgent");
            
            // Add provider name
            promptBuilder.Append($"[{currentProvider}]");
            
            // Add model name for Ollama
            if (currentProvider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var ollamaOptions = _serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CodeAgent.Providers.Ollama.OllamaOptions>>();
                    var currentModel = ollamaOptions.Value.DefaultModel ?? "llama3.2";
                    promptBuilder.Append($":{currentModel}");
                }
                catch
                {
                    // Fallback if Ollama options aren't available
                }
            }
            
            promptBuilder.Append("$ ");
            return promptBuilder.ToString();
        }
        catch
        {
            // Fallback to original prompt on any error
            return _prompt;
        }
    }

    private async Task<bool> Execute(IAnsiConsole console)
    {
        var input = _currentLine.ToString().Trim();
        
        // Return to latest history
        _historyIndex = -1;

        // Handle special cases
        if (string.IsNullOrWhiteSpace(input)) return true;
        
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
            
            // Handle exit command
            if (commandLine.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                return false;
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
        
        try
        {
            // Process the message without status display to avoid conflicts with permission prompts
            var response = await _chatService.ProcessMessageAsync(message);
            
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
        
        // Move cursor back one position
        console.Cursor.Move(CursorDirection.Left, 1);
        
        // Clear from cursor to end of line
        console.Write("\x1b[K");
        
        // Rewrite the rest of the line if there's text after cursor
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
        // Get current prompt length (without ANSI markup)
        var currentPromptText = BuildDynamicPrompt();
        var totalMoveLeft = _cursorIndex + currentPromptText.Length;
        console.Cursor.Move(CursorDirection.Left, totalMoveLeft);
        
        // Clear the entire line from start
        console.Write("\x1b[K");
        
        _currentLine.Clear();
        _cursorIndex = 0;
        
        // Redisplay the prompt
        WritePrompt(console);

        if (!string.IsNullOrWhiteSpace(line))
        {
            _currentLine.Append(line);
            console.Write(_currentLine.ToString());
            _cursorIndex = _currentLine.Length;
        }
    }

    private void AutoCompleteCommand(IAnsiConsole console)
    {
        var currentCommand = _currentLine.ToString().Substring(1); // Remove the '/' prefix
        var availableCommands = new[] { "help", "scan", "provider", "model", "prompt", "config", "setup", "mcp", "clear", "exit" };
        
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

    private void MoveCursorToStart(IAnsiConsole console)
    {
        if (_cursorIndex == 0) return;
        console.Cursor.Move(CursorDirection.Left, _cursorIndex);
        _cursorIndex = 0;
    }

    private void MoveCursorToEnd(IAnsiConsole console)
    {
        if (_cursorIndex == _currentLine.Length) return;
        var moveCount = _currentLine.Length - _cursorIndex;
        console.Cursor.Move(CursorDirection.Right, moveCount);
        _cursorIndex = _currentLine.Length;
    }

    private void ClearLine(IAnsiConsole console)
    {
        // Move to start and clear entire input
        MoveCursorToStart(console);
        console.Write("\x1b[K"); // Clear from cursor to end of line
        _currentLine.Clear();
        _cursorIndex = 0;
    }

    private void DeleteToEndOfLine(IAnsiConsole console)
    {
        if (_cursorIndex >= _currentLine.Length) return;
        
        // Remove text from cursor to end
        _currentLine.Remove(_cursorIndex, _currentLine.Length - _cursorIndex);
        console.Write("\x1b[K"); // Clear from cursor to end of line
    }

    private void DeleteCharacter(IAnsiConsole console)
    {
        if (_cursorIndex >= _currentLine.Length) return;
        
        _currentLine.Remove(_cursorIndex, 1);
        
        // Redraw from current position
        var textToWrite = _currentLine.ToString(_cursorIndex, _currentLine.Length - _cursorIndex) + " ";
        console.Write(textToWrite);
        console.Cursor.Move(CursorDirection.Left, textToWrite.Length);
    }

    private void DeleteWordBackwards(IAnsiConsole console)
    {
        if (_cursorIndex == 0) return;
        
        var startPos = _cursorIndex;
        
        // Skip whitespace backwards
        while (_cursorIndex > 0 && char.IsWhiteSpace(_currentLine[_cursorIndex - 1]))
        {
            _cursorIndex--;
        }
        
        // Delete non-whitespace characters backwards
        while (_cursorIndex > 0 && !char.IsWhiteSpace(_currentLine[_cursorIndex - 1]))
        {
            _cursorIndex--;
        }
        
        var deleteCount = startPos - _cursorIndex;
        if (deleteCount > 0)
        {
            _currentLine.Remove(_cursorIndex, deleteCount);
            console.Cursor.Move(CursorDirection.Left, deleteCount);
            
            // Redraw line from current position
            var textToRewrite = _currentLine.ToString(_cursorIndex, _currentLine.Length - _cursorIndex);
            var padding = new string(' ', deleteCount);
            console.Write(textToRewrite + padding);
            console.Cursor.Move(CursorDirection.Left, textToRewrite.Length + deleteCount);
        }
    }

    private void MoveWordLeft(IAnsiConsole console)
    {
        if (_cursorIndex == 0) return;
        
        var originalPos = _cursorIndex;
        
        // Skip whitespace backwards
        while (_cursorIndex > 0 && char.IsWhiteSpace(_currentLine[_cursorIndex - 1]))
        {
            _cursorIndex--;
        }
        
        // Move through non-whitespace characters
        while (_cursorIndex > 0 && !char.IsWhiteSpace(_currentLine[_cursorIndex - 1]))
        {
            _cursorIndex--;
        }
        
        var moveCount = originalPos - _cursorIndex;
        if (moveCount > 0)
        {
            console.Cursor.Move(CursorDirection.Left, moveCount);
        }
    }

    private void MoveWordRight(IAnsiConsole console)
    {
        if (_cursorIndex >= _currentLine.Length) return;
        
        var originalPos = _cursorIndex;
        
        // Skip whitespace forwards
        while (_cursorIndex < _currentLine.Length && char.IsWhiteSpace(_currentLine[_cursorIndex]))
        {
            _cursorIndex++;
        }
        
        // Move through non-whitespace characters
        while (_cursorIndex < _currentLine.Length && !char.IsWhiteSpace(_currentLine[_cursorIndex]))
        {
            _cursorIndex++;
        }
        
        var moveCount = _cursorIndex - originalPos;
        if (moveCount > 0)
        {
            console.Cursor.Move(CursorDirection.Right, moveCount);
        }
    }

    private async Task<int> ProcessPipedInput(IAnsiConsole console)
    {
        // Skip the welcome message when processing piped input
        string? line;
        var exitCode = 0;
        
        while ((line = await Console.In.ReadLineAsync()) != null)
        {
            // Process each line of piped input
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            // Add to history
            if (_history.Count == 0 || !_history[0].Equals(line, StringComparison.OrdinalIgnoreCase))
            {
                _history.Insert(0, line);
            }
            
            // Process as command or chat message
            if (line.StartsWith(_settings.CommandPrefix))
            {
                // It's a command - remove the prefix and execute
                var commandLine = line.Substring(_settings.CommandPrefix.Length).Trim();
                if (string.IsNullOrWhiteSpace(commandLine))
                {
                    commandLine = "help";
                }
                
                // Handle exit command
                if (commandLine.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                
                var args = ProcessCommandLine(commandLine);
                try
                {
                    exitCode = await _commandApp.RunAsync(args);
                }
                catch (Exception ex)
                {
                    console.MarkupLine($"[red]Command error: {ex.Message}[/]");
                    exitCode = 1;
                }
            }
            else if (line.Equals(InternalCommands.Clear, StringComparison.OrdinalIgnoreCase))
            {
                console.Clear();
                _chatService.ClearHistory();
                console.MarkupLine("[green]Screen and chat history cleared.[/]");
            }
            else
            {
                // It's a chat message
                console.MarkupLine($"[bold cyan]User:[/] {line}");
                await ProcessChatMessage(console, line);
            }
        }
        
        return exitCode;
    }
}