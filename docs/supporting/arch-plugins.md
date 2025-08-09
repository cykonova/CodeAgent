# Plugin Architecture

## Extension Points

Plugins extend interfaces through event-driven architecture:

| Interface | Extension Capabilities |
|-----------|----------------------|
| Web UI | Nx.dev library modules, Angular components, event subscriptions |
| CLI | Commands, flags, formatters, event handlers |
| IDE | Commands, views, providers, event listeners |

## Plugin Lifecycle

1. Discovery - Find Nx.dev libraries or installed plugins
2. Loading - Load plugin manifest and event subscriptions
3. Registration - Register event handlers and extensions
4. Initialization - Setup plugin services and event bus connections
5. Activation - Enable UI/commands and start event listening
6. Runtime - Process events and handle user interactions
7. Deactivation - Clean shutdown

## Extension API

The unified extension API allows plugins to register capabilities:

| Method | Purpose |
|--------|---------|
| RegisterCommand | Add CLI/IDE command |
| RegisterView | Add UI view/panel |
| RegisterProvider | Add LLM provider |
| RegisterAction | Add context action |
| RegisterFormatter | Add output formatter |

## Cross-Interface Communication

Plugins can communicate across interfaces:
- Web UI ↔ Backend via WebSocket
- CLI ↔ Backend via API calls  
- IDE ↔ Backend via Language Server Protocol
- Plugin ↔ Plugin via event bus

## Permission Model

| Permission Level | Access |
|-----------------|--------|
| Basic | Read config, emit events |
| Extended | Network, filesystem |
| Privileged | Docker, system commands |
| Admin | Modify core behavior |

## Plugin Distribution

- Official registry for verified plugins
- Local file installation
- Git repository references
- Auto-update capability