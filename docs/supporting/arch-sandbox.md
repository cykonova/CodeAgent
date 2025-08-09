# Sandbox Architecture

## Purpose
Provides isolated environments where coding agents execute development tasks and present results to users.

## Container Components

| Component | Purpose |
|-----------|---------|
| Coding Agent CLI | Executes commands in non-interactive mode |
| Workspace | Persistent directory for project files |
| Dev Server | Hosts web applications for preview |
| Artifact Manager | Handles output presentation |

## Agent Execution Model
- Agent runs in non-interactive CLI mode
- Commands executed via sandbox manager
- Permission requests proxied to user interface
- User approves/denies elevated operations
- Output streamed back to user interface
- Artifacts exposed through configured channels

## Artifact Presentation

| Type | Method | User Access |
|------|--------|------------|
| Web App | Port forward | Browser preview |
| API | Endpoint exposure | Interactive testing |
| Files | Volume sync | Direct download |
| Logs | Stream | Real-time view |
| Reports | Static hosting | Read-only access |

## Resource Management

| Resource | Default | Configurable |
|----------|---------|--------------|
| Memory | 4GB | Yes |
| CPU | 2 cores | Yes |
| Disk | 10GB | Yes |
| Network | Restricted | Yes |
| Processes | Limited | Yes |

## Permission Management

| Operation | Default | Requires Approval |
|-----------|---------|------------------|
| Workspace R/W | Allowed | No |
| Package install | Allowed | No |
| External network | Denied | Yes |
| Port exposure | Denied | Yes |
| Resource increase | Denied | Yes |
| System access | Denied | Always denied |

## Lifecycle Stages
1. Container creation with workspace
2. Agent CLI installation
3. Task execution with permission checks
4. User approval for elevated operations
5. Artifact generation
6. Result presentation
7. Cleanup or persistence