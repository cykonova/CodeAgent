# Plugin Architecture

## Extension Points

Plugins can extend multiple interfaces through a unified API:

| Interface | Extension Capabilities |
|-----------|----------------------|
| Web UI | Pages, widgets, menus, actions |
| CLI | Commands, flags, formatters |
| IDE | Commands, views, providers |

## Plugin Lifecycle

1. Discovery - Find installed plugins
2. Loading - Load plugin manifest
3. Registration - Register extensions
4. Initialization - Setup plugin services
5. Activation - Enable UI/commands
6. Runtime - Handle user interactions
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