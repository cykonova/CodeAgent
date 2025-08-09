# CLI Architecture

## Command Structure
```
codeagent
├── project
│   ├── create
│   ├── list
│   ├── delete
│   └── config
├── provider
│   ├── add
│   ├── test
│   └── list
├── sandbox
│   ├── create
│   ├── exec
│   ├── logs
│   └── shell
├── config
│   ├── get
│   ├── set
│   └── list
└── chat (interactive mode)
```

## Command Parser
```csharp
public class CommandParser
{
    private readonly Dictionary<string, ICommand> _commands;
    
    public async Task<int> Execute(string[] args)
    {
        var command = ParseCommand(args);
        var options = ParseOptions(args);
        
        if (_commands.TryGetValue(command, out var handler))
        {
            return await handler.ExecuteAsync(options);
        }
        
        return ShowHelp();
    }
}
```

## Configuration Management
```csharp
public class ConfigManager
{
    private readonly string _configPath = "~/.codeagent/config.yaml";
    
    public T Get<T>(string key);
    public void Set(string key, object value);
    public void Save();
    public void Load();
}
```

## Interactive Mode
```csharp
public class InteractiveMode
{
    private readonly WebSocketClient _client;
    private readonly ConsoleRenderer _renderer;
    
    public async Task Run()
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (input == "exit") break;
            
            var response = await _client.SendCommand(input);
            _renderer.RenderResponse(response);
        }
    }
}
```

## Output Formatting
- Table format for lists
- JSON output option
- Progress bars
- Colored output
- Markdown rendering