# Frontend Architecture

## Nx Monorepo Structure

The frontend uses Nx.dev with Module Federation for scalable micro-frontend architecture.

### Applications
- **Shell**: Main container with routing and theme
- **Dashboard**: Metrics and overview remote
- **Projects**: Project management remote  
- **Chat**: Agent interaction remote
- **Settings**: Configuration remote

### Libraries
- **UI Components**: Reusable Material components
- **Data Access**: API and state management
- **WebSocket**: Real-time communication
- **Theme**: Material theming system
- **Utils**: Shared utilities

## Angular Material Standards

| Component Type | Material Component | Usage |
|---------------|-------------------|--------|
| Forms | mat-form-field | All input fields |
| Tables | mat-table | Data grids |
| Navigation | mat-sidenav, mat-toolbar | App layout |
| Dialogs | mat-dialog | Modal windows |
| Lists | mat-list | Item displays |
| Cards | mat-card | Content containers |

## Development Standards

### Code Organization
- Maximum 100 lines per file
- One component per file
- Logical separation of concerns
- Shared logic in libraries

### Theming Rules
- No hardcoded colors
- Use Material theme variables
- Support light/dark modes
- Custom components follow theme system

### Component Guidelines
- All components standalone
- Built in library projects
- Fully typed interfaces
- Unit tested with 80% coverage

## Module Federation Config

Each remote application is independently deployable and loaded at runtime by the shell application. Communication between remotes happens through:
- Shared services in libraries
- Event bus for cross-app messaging
- Shared state management
- Common WebSocket connection