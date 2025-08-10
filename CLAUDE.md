# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Code Agent is a comprehensive development assistant platform that integrates multiple LLM providers through a unified interface. The system follows a **zero-configuration by default** principle - users can start immediately with just a provider configured.

## Architecture

### Technology Stack
- **Backend**: .NET 8 (C#) with minimal APIs
- **Frontend**: Angular with Nx.dev (TypeScript) and Module Federation
- **UI Framework**: Angular Material with strict theming standards
- **CLI**: .NET with Spectre.Console for rich terminal output
- **Communication**: WebSocket gateway for real-time messaging
- **Containerization**: Docker with sandbox isolation for agent execution

### Core Components
1. **WebSocket Gateway**: Central communication hub handling all client connections
2. **Provider Management**: Registry for LLM providers (Anthropic, OpenAI, Ollama)
3. **Agent System**: Orchestrator for multi-agent coordination and workflows
4. **Docker Sandbox**: Secure execution environment with MCP support
5. **Project Management**: Configuration inheritance and workflow templates

## Development Commands

### Backend (.NET 8)
```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Start the backend server
dotnet run --project src/CodeAgent.Gateway

# Run with hot reload
dotnet watch run --project src/CodeAgent.Gateway
```

### Frontend (Angular/Nx)
```bash
# Install dependencies
npm install

# Serve development server
nx serve shell

# Build for production
nx build shell --configuration=production

# Run unit tests
nx test shell

# Run specific library tests
nx test ui-components

# Lint check
nx lint shell

# Format code
nx format:write
```

### Docker Operations
```bash
# Build containers
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f [service-name]

# Stop services
docker-compose down
```

## Project Structure

### Backend Organization
- `src/CodeAgent.Gateway/` - WebSocket gateway and routing
- `src/CodeAgent.Providers/` - LLM provider integrations
- `src/CodeAgent.Agents/` - Agent orchestration services
- `src/CodeAgent.Sandbox/` - Docker sandbox management
- `src/CodeAgent.Projects/` - Project management services

### Frontend Organization (Nx Monorepo)
- `src/frontend/apps/shell/` - Main container application
- `src/frontend/apps/dashboard/` - Metrics remote module
- `src/frontend/apps/projects/` - Project management remote
- `src/frontend/apps/chat/` - Agent interaction remote
- `src/frontend/libs/ui-components/` - Reusable Material components
- `src/frontend/libs/data-access/` - API and state management
- `src/frontend/libs/websocket/` - Real-time communication

## Development Standards

### Angular Material Requirements
- Use Material components for ALL UI elements
- No hardcoded colors - use theme variables only
- Support light/dark mode switching
- All text must be externalized for i18n
- Maximum 100 lines per component file

### Backend Standards
- Minimal API pattern for all endpoints
- Dependency injection for all services
- Channel-based async communication
- JWT authentication with rate limiting

### Testing Requirements
- Unit test coverage minimum: 80%
- Integration tests for all API endpoints
- WebSocket connection tests required
- Mock providers for external services

## Implementation Status

The project is organized in 10 phases (see `docs/00-project-status.md`). Each phase has:
- Primary documentation in `docs/[phase-number]-[name].md`
- Supporting architecture docs in `docs/supporting/arch-*.md`

When implementing a phase:
1. Update status in `00-project-status.md`
2. Follow the specific phase documentation
3. Reference supporting docs only as needed
4. Create implementation in appropriate project structure

## Key Design Decisions

1. **Module Federation**: Each frontend app is independently deployable
2. **Event-Driven**: All communication through WebSocket events
3. **Plugin Architecture**: Extensible through standardized plugin manifests
4. **Security Levels**: Configurable sandbox isolation (None/Container/VM)
5. **Provider Agnostic**: Unified interface for all LLM providers

## Common Development Tasks

### Adding a New Provider
1. Implement `IProvider` interface in `CodeAgent.Providers`
2. Register in `ProviderRegistry`
3. Add configuration schema
4. Create UI configuration component

### Creating a New Agent Type
1. Define agent configuration in `AgentTypes/`
2. Implement execution logic in `AgentOrchestrator`
3. Add workflow templates if needed
4. Update agent selection UI

### Adding a CLI Command
1. Create command class inheriting from `BaseCommand`
2. Use Spectre.Console for all output
3. Add to command registration
4. Include help text and examples

- Angular 20 web frontend
- Use theme based padding and margins, in cases where theme padding/margin isn't enough add calc relative to the theme spacing.