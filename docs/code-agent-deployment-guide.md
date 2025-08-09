# Code Agent - Local Deployment Guide

## Table of Contents
1. [Quick Start](#quick-start)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [IDE Setup](#ide-setup)
5. [Provider Setup](#provider-setup)
6. [Troubleshooting](#troubleshooting)
7. [Development](#development)

## Quick Start

### One-Line Install
```bash
# Linux/Mac
curl -sSL https://raw.githubusercontent.com/your-org/code-agent/main/install.sh | bash

# Windows (PowerShell as Admin)
iwr -useb https://raw.githubusercontent.com/your-org/code-agent/main/install.ps1 | iex
```

### Manual Quick Start
```bash
# Clone repository
git clone https://github.com/your-org/code-agent.git
cd code-agent

# Start with default configuration
docker-compose up -d

# Open web interface
open http://localhost:8080

# Or use CLI
./codeagent /help
```

## Installation

### System Requirements

#### Minimum Requirements
- **CPU**: 2 cores
- **RAM**: 4 GB
- **Storage**: 10 GB free space
- **OS**: Windows 10+, macOS 10.15+, Ubuntu 20.04+
- **Docker**: 20.10+ with Docker Compose

#### Recommended for Best Performance
- **CPU**: 4+ cores
- **RAM**: 8 GB
- **Storage**: 20 GB free space (more if using local models)
- **GPU**: Optional, for local model acceleration

### Installing Docker

#### Windows
1. Download [Docker Desktop](https://www.docker.com/products/docker-desktop/)
2. Run installer
3. Enable WSL 2 backend during setup
4. Restart computer

#### macOS
```bash
# Using Homebrew
brew install --cask docker

# Or download Docker Desktop from docker.com
```

#### Linux
```bash
# Ubuntu/Debian
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
# Log out and back in

# Fedora
sudo dnf install docker docker-compose
sudo systemctl start docker
sudo systemctl enable docker
```

### Installing Code Agent

#### Option 1: Pre-built Release
```bash
# Download latest release
wget https://github.com/your-org/code-agent/releases/latest/download/code-agent.tar.gz
tar -xzf code-agent.tar.gz
cd code-agent

# Run setup
./setup.sh
```

#### Option 2: Build from Source
```bash
# Clone repository
git clone https://github.com/your-org/code-agent.git
cd code-agent

# Build containers
docker-compose build

# Start services
docker-compose up -d
```

## Configuration

### Basic Configuration

Create a `.env` file in the code-agent directory:

```bash
# .env file for local deployment

# Core Settings
WEB_PORT=8080
API_PORT=8081
WEBSOCKET_PORT=8082

# Storage (relative or absolute paths)
DATA_PATH=./data
PLUGINS_PATH=./plugins
PROJECTS_PATH=./projects

# Default Provider Settings
DEFAULT_PROVIDER=anthropic
DEFAULT_PLANNING_PROVIDER=anthropic
DEFAULT_CODING_PROVIDER=openai
DEFAULT_REVIEW_PROVIDER=anthropic
DEFAULT_TESTING_PROVIDER=gemini
DEFAULT_DOCS_PROVIDER=mistral

# API Provider Keys (optional - can configure via UI)
ANTHROPIC_API_KEY=your-key-here
OPENAI_API_KEY=your-key-here
GEMINI_API_KEY=your-key-here
GROK_API_KEY=your-key-here
MISTRAL_API_KEY=your-key-here
COHERE_API_KEY=your-key-here

# Local Model Providers
OLLAMA_HOST=http://host.docker.internal:11434
LMSTUDIO_HOST=http://host.docker.internal:1234

# Cost Management
MONTHLY_COST_LIMIT=100.00
DEFAULT_PROJECT_COST_LIMIT=10.00

# Optional Settings
LOG_LEVEL=info
MAX_CONTEXT_SIZE=32000
ENABLE_TELEMETRY=false
PREFER_LOCAL_MODELS=false
```

### docker-compose.yml (With Sandbox Support)
```yaml
version: '3.8'

services:
  code-agent:
    image: codeagent/main:latest
    container_name: code-agent
    ports:
      - "${WEB_PORT:-8080}:8080"     # Web UI
      - "${API_PORT:-8081}:8081"     # REST API
      - "${WEBSOCKET_PORT:-8082}:8082" # WebSocket
    volumes:
      - ${DATA_PATH:-./data}:/app/data
      - ${PLUGINS_PATH:-./plugins}:/app/plugins
      - ${PROJECTS_PATH:-./projects}:/app/projects
      # Docker socket for sandbox management
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - DEFAULT_PROVIDER=${DEFAULT_PROVIDER:-ollama}
      - LOG_LEVEL=${LOG_LEVEL:-info}
      # Docker MCP Configuration
      - DOCKER_MCP_ENABLED=true
      - DOCKER_SANDBOX_ENABLED=true
      - DOCKER_LLM_ENABLED=${DOCKER_LLM_ENABLED:-false}
    restart: unless-stopped
    extra_hosts:
      - "host.docker.internal:host-gateway"
    networks:
      - codeagent-net
      - sandbox-net

  # Optional: Include Ollama for local models
  ollama:
    image: ollama/ollama:latest
    container_name: ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama-data:/root/.ollama
    restart: unless-stopped
    networks:
      - codeagent-net

  # Optional: Docker LLM Gateway (Beta)
  docker-llm:
    image: docker-llm/gateway:beta
    container_name: docker-llm
    profiles: ["docker-llm"]
    ports:
      - "8090:8090"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - llm-models:/models
    environment:
      - MCP_ENABLED=true
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
    networks:
      - codeagent-net

networks:
  codeagent-net:
    driver: bridge
  sandbox-net:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16

volumes:
  ollama-data:
  llm-models:
```

### First Run Setup

1. **Start the services:**
```bash
docker-compose up -d
```

2. **Access the web interface:**
```bash
open http://localhost:8080
```

3. **Run initial setup wizard** (via web UI or CLI):
```bash
# CLI setup
./codeagent setup

# This will prompt for:
# - Default provider selection
# - API keys (if using cloud providers)
# - IDE integration preferences
# - Project directory
```

## IDE Setup

### Visual Studio Code

#### Installation
```bash
# Install from marketplace
code --install-extension code-agent.code-agent-vscode

# Or manually
1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "Code Agent"
4. Click Install
```

#### Configuration
```json
// .vscode/settings.json
{
  "codeAgent.serverUrl": "ws://localhost:8082",
  "codeAgent.defaultCommand": "/code",
  "codeAgent.autoConnect": true,
  "codeAgent.showInlineHints": true
}
```

#### Usage
- Command Palette: `Ctrl+Shift+P` → "Code Agent: "
- Quick Access: `Alt+C` (customizable)
- Context Menu: Right-click in editor → "Code Agent"

### Visual Studio

#### Installation
1. Download the VSIX from releases
2. Double-click to install
3. Restart Visual Studio

#### Configuration
Tools → Options → Code Agent:
- Server URL: `ws://localhost:8082`
- Enable inline suggestions
- Set keyboard shortcuts

### JetBrains IDEs

#### Installation
1. File → Settings → Plugins
2. Search for "Code Agent"
3. Install and restart

#### Configuration
```xml
<!-- .idea/codeagent.xml -->
<component name="CodeAgentSettings">
  <option name="serverUrl" value="ws://localhost:8082" />
  <option name="enabled" value="true" />
</component>
```

## Provider Setup

### Local Models (Recommended for Privacy)

#### Ollama Setup
```bash
# Install Ollama (if not using Docker version)
curl -fsSL https://ollama.ai/install.sh | sh

# Pull models
ollama pull llama3.1
ollama pull codellama
ollama pull mixtral

# Verify
ollama list
```

#### LM Studio Setup
1. Download from [lmstudio.ai](https://lmstudio.ai)
2. Install and start LM Studio
3. Load desired models
4. Enable server mode on port 1234
5. Configure in Code Agent:
```bash
./codeagent config set provider lmstudio
./codeagent config set lmstudio.url http://localhost:1234
```

### API Providers (Optional)

#### Configure via CLI
```bash
# Anthropic
./codeagent config set provider anthropic
./codeagent config set anthropic.api_key YOUR_KEY

# OpenAI
./codeagent config set provider openai
./codeagent config set openai.api_key YOUR_KEY

# List configured providers
./codeagent provider list
```

#### Configure via Web UI
1. Navigate to http://localhost:8080/settings
2. Click "Providers"
3. Add/configure providers
4. Test connection

## Docker Sandbox Setup

### Enable Sandboxing
```bash
# Enable Docker sandboxing for all projects
./codeagent config set sandbox.enabled true

# Configure default sandbox settings
./codeagent config set sandbox.image codeagent/sandbox:latest
./codeagent config set sandbox.memory 4G
./codeagent config set sandbox.cpus 2

# Set security level
./codeagent config set sandbox.security development
```

### Configure Docker Desktop MCP (Beta)
```bash
# Enable Docker Desktop MCP integration
./codeagent mcp enable docker-desktop

# Test MCP connection
./codeagent mcp test docker

# List available MCP tools
./codeagent mcp tools list
```

### Configure Docker LLM (Beta)
```bash
# Enable Docker LLM provider
./codeagent provider add docker-llm \
  --type docker \
  --endpoint http://localhost:8090

# Add containerized model
./codeagent docker-llm add-model \
  --name llama3-gpu \
  --image docker-llm/llama3:latest \
  --gpus 1 \
  --memory 16G
```

### Project Sandbox Management
```bash
# Create sandbox for project
./codeagent project sandbox create my-app \
  --security-level development \
  --network bridge

# Execute command in sandbox
./codeagent sandbox exec my-app -- npm install

# View sandbox logs
./codeagent sandbox logs my-app --follow

# Access sandbox shell
./codeagent sandbox shell my-app

# Stop sandbox (keeps data)
./codeagent sandbox stop my-app

# Remove sandbox (deletes container)
./codeagent sandbox remove my-app
```

## Troubleshooting

### Common Issues

#### Port Already in Use
```bash
# Check what's using the port
lsof -i :8080  # Mac/Linux
netstat -ano | findstr :8080  # Windows

# Change port in .env file
WEB_PORT=8090
docker-compose up -d
```

#### Cannot Connect to Docker
```bash
# Linux: Add user to docker group
sudo usermod -aG docker $USER
# Log out and back in

# Mac/Windows: Ensure Docker Desktop is running
```

#### Provider Connection Failed
```bash
# Test local Ollama
curl http://localhost:11434/api/tags

# Test from container
docker exec code-agent curl http://host.docker.internal:11434/api/tags

# Check logs
docker logs code-agent
```

#### Low Performance
```bash
# Increase memory allocation
docker update code-agent --memory="4g"

# For local models, use GPU acceleration
docker run --gpus all ...  # If NVIDIA GPU available
```

### Getting Logs
```bash
# View logs
docker logs code-agent --tail 50 -f

# Export logs
docker logs code-agent > codeagent.log 2>&1

# Increase log verbosity
docker exec code-agent codeagent config set log_level debug
```

### Reset/Clean Install
```bash
# Stop services
docker-compose down

# Remove data (warning: deletes all data)
rm -rf ./data

# Fresh start
docker-compose up -d
```

## Development

### Building from Source

#### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Git

#### Build Steps
```bash
# Clone repository
git clone https://github.com/your-org/code-agent.git
cd code-agent

# Build backend
cd src
dotnet build
dotnet publish -c Release -o ../dist

# Build frontend
cd web
npm install
npm run build

# Build Docker image
cd ../..
docker build -t codeagent/main:local .
```

### Running in Development Mode
```bash
# Backend (hot reload enabled)
cd src
dotnet watch run

# Frontend (separate terminal)
cd web
npm run dev

# Access at http://localhost:3000 (frontend)
# API at http://localhost:5000 (backend)
```

## Plugin Installation & Configuration

### Installing Plugins

#### Via CLI
```bash
# Install from registry
./codeagent plugin install docker-mcp-tools

# Install from file
./codeagent plugin install ./my-plugin.zip

# Install from URL
./codeagent plugin install https://github.com/user/plugin/releases/latest

# List installed plugins
./codeagent plugin list

# Update plugin
./codeagent plugin update docker-mcp-tools
```

#### Via Web UI
1. Navigate to Settings → Plugins
2. Browse available plugins
3. Click "Install" on desired plugin
4. Configure plugin settings
5. Enable/disable as needed

### Core Plugins

#### Docker MCP Tools Plugin
```bash
# Install
./codeagent plugin install docker-mcp-tools

# Configure
./codeagent plugin config docker-mcp-tools \
  --docker-socket /var/run/docker.sock \
  --enable-compose true \
  --enable-swarm false
```

#### Docker LLM Provider Plugin
```bash
# Install
./codeagent plugin install docker-llm-provider

# Configure
./codeagent plugin config docker-llm-provider \
  --gateway-url http://localhost:8090 \
  --enable-gpu true
```

#### Sandbox Manager Plugin
```bash
# Install
./codeagent plugin install sandbox-manager

# Configure
./codeagent plugin config sandbox-manager \
  --default-image codeagent/sandbox:latest \
  --pool-size 5 \
  --idle-timeout 30m
```

### Creating Custom Plugins

#### Plugin Structure
```
my-plugin/
├── manifest.json
├── index.js (or index.cs for C# plugins)
├── README.md
└── config.schema.json
```

#### Example Plugin Manifest
```json
{
  "name": "my-custom-provider",
  "version": "1.0.0",
  "type": "llm-provider",
  "main": "index.js",
  "author": "Your Name",
  "description": "Custom LLM provider",
  "configuration": {
    "endpoint": {
      "type": "string",
      "required": true,
      "description": "API endpoint"
    }
  },
  "permissions": [
    "network.external",
    "filesystem.read"
  ]
}
```

#### Deploy Plugin
```bash
# Package plugin
zip -r my-plugin.zip my-plugin/

# Install locally
./codeagent plugin install ./my-plugin.zip

# Publish to registry (if available)
./codeagent plugin publish ./my-plugin.zip
```

### Contributing
1. Fork the repository
2. Create feature branch
3. Make changes
4. Run tests: `dotnet test`
5. Submit pull request

## Advanced Configuration

### Multi-User Setup (Small Teams)

Add basic authentication:
```yaml
# docker-compose.yml addition
environment:
  - ENABLE_AUTH=true
  - AUTH_MODE=basic
  - ADMIN_PASSWORD=changeme
```

### Resource Limits
```yaml
# docker-compose.yml
services:
  code-agent:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G
        reservations:
          memory: 2G
```

### Custom Model Configuration
```json
// config/models.json
{
  "providers": {
    "ollama": {
      "models": {
        "codellama": {
          "contextSize": 16384,
          "temperature": 0.7,
          "systemPrompt": "You are a helpful coding assistant..."
        }
      }
    }
  }
}
```

## Getting Help

### Resources
- **Documentation**: https://github.com/your-org/code-agent/wiki
- **Issues**: https://github.com/your-org/code-agent/issues
- **Discussions**: https://github.com/your-org/code-agent/discussions
- **Discord**: [Join our community](https://discord.gg/codeagent)

### Debugging Commands
```bash
# Check version
./codeagent version

# Run diagnostics
./codeagent diagnose

# Test provider connection
./codeagent provider test ollama

# Validate configuration
./codeagent config validate
```

---
*Version: 1.0*  
*License: MIT*  
*Repository: https://github.com/your-org/code-agent*