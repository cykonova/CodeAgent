# Code Agent Project Overview

## Purpose
Code Agent is a comprehensive development assistant platform that integrates multiple LLM providers through a unified interface. The system follows a **zero-configuration by default** principle - users can start immediately with just a provider configured.

## Tech Stack
- **Backend**: .NET 8 (C#) with minimal APIs
- **Frontend**: Angular 20.1 with Nx.dev (TypeScript) and Module Federation
- **UI Framework**: Angular Material with strict theming standards
- **CLI**: .NET with Spectre.Console for rich terminal output
- **Communication**: WebSocket gateway for real-time messaging
- **Containerization**: Docker with sandbox isolation for agent execution

## Architecture Components
1. **WebSocket Gateway**: Central communication hub handling all client connections
2. **Provider Management**: Registry for LLM providers (Anthropic, OpenAI, Ollama)
3. **Agent System**: Orchestrator for multi-agent coordination and workflows
4. **Docker Sandbox**: Secure execution environment with MCP support
5. **Project Management**: Configuration inheritance and workflow templates

## Project Structure
- Backend: .NET 8 projects (Gateway, Core, Shared)
- Frontend: Nx monorepo with Angular apps
- Documentation: Phased implementation docs in docs/
- 10 implementation phases from core infrastructure to deployment