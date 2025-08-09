# CLI Architecture

## Spectre.Console Integration

The CLI uses Spectre.Console for rich terminal output with formatted tables, progress bars, interactive prompts, and styled text.

## Command Class Architecture

Each command and subcommand is a separate class:
- All commands inherit from `BaseCommand`
- Subcommands are nested classes or separate files
- Dependency injection for services
- Async execution support
- AnsiConsole for all output

## Output Capabilities

| Feature | Spectre.Console Component | Usage |
|---------|---------------------------|-------|
| Tables | AnsiConsole.Table | List displays |
| Progress | AnsiConsole.Progress | Long operations |
| Trees | AnsiConsole.Tree | Hierarchical data |
| Prompts | AnsiConsole.Prompt | User input |
| Markup | AnsiConsole.Markup | Styled text |
| Panels | AnsiConsole.Panel | Error messages |
| Rules | AnsiConsole.Rule | Visual separation |
| Live | AnsiConsole.Live | Real-time updates |

## Command Organization

```
Commands/
├── BaseCommand.cs              # Abstract base class
├── Project/
│   ├── ProjectCommand.cs       # Parent command
│   ├── CreateCommand.cs        # Subcommand
│   ├── ListCommand.cs          # Subcommand
│   └── ConfigCommand.cs        # Subcommand
├── Provider/
│   ├── ProviderCommand.cs
│   └── [subcommands]
└── Chat/
    └── ChatCommand.cs          # Interactive mode
```

## Error Handling

- Exceptions caught and displayed in formatted panels
- User-friendly error messages with suggestions
- Debug mode shows full stack traces
- Color-coded severity levels