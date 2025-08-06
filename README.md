# CodeAgent

AI-powered coding assistant with support for multiple LLM providers.

## Features

- 🤖 Multiple LLM provider support (OpenAI, Claude, Ollama)
- 💬 Interactive chat interface with markdown rendering
- 📁 Project file scanning and analysis
- 🔧 Configurable provider settings
- 🎨 Rich terminal UI with Spectre.Console
- 📜 Command history and persistence
- 🔌 Model Context Protocol (MCP) support
- 🚀 Pipe input support for automation

## Installation

### As a .NET Global Tool

```bash
# Install from GitHub Packages
dotnet tool install --global Cykonova.CodeAgent --version 0.1.0-alpha.1 --add-source https://nuget.pkg.github.com/cykonova/index.json

# Or install latest alpha
dotnet tool install --global Cykonova.CodeAgent --prerelease --add-source https://nuget.pkg.github.com/cykonova/index.json
```

### From Source

```bash
git clone https://github.com/cykonova/CodeAgent.git
cd CodeAgent
dotnet build
dotnet run --project src/CodeAgent.CLI
```

## Usage

### Interactive Mode

```bash
codeagent
```

This launches the interactive shell where you can:
- Chat with the AI assistant
- Run commands with `/` prefix (e.g., `/help`, `/scan`, `/setup`)
- Use arrow keys for command history
- Tab completion for commands

### Command Mode

```bash
# Scan project files
codeagent scan

# Configure providers
codeagent setup

# Get help
codeagent --help
```

### Piped Input

```bash
# Send a chat message
echo "Explain this code" | codeagent

# Run a command
echo "/scan" | codeagent

# Multiple commands
echo -e "/scan\n/help" | codeagent
```

## Configuration

CodeAgent stores configuration in `~/.codeagent/settings.json`. You can configure:

- Default LLM provider
- API keys for OpenAI and Claude
- Ollama server URL
- Model preferences

Run `/setup` in interactive mode or `codeagent setup` to configure providers.

## Supported Providers

### OpenAI
- GPT-4, GPT-4 Turbo, GPT-3.5 Turbo
- Requires API key from [OpenAI Platform](https://platform.openai.com)

### Claude (Anthropic)
- Claude 3 Opus, Sonnet, Haiku
- Requires API key from [Anthropic Console](https://console.anthropic.com)

### Ollama
- Local models (Llama, Mistral, etc.)
- Requires [Ollama](https://ollama.ai) running locally

## Development

### Prerequisites

- .NET 8.0 SDK
- Git

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Creating a Package

```bash
dotnet pack src/CodeAgent.CLI/CodeAgent.CLI.csproj -c Release
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built with [Spectre.Console](https://spectreconsole.net/) for rich terminal UI
- Inspired by AI coding assistants and CLI tools
- Uses [Markdig](https://github.com/xoofx/markdig) for markdown rendering