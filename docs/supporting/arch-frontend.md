# Frontend Architecture

## Component Structure
```
web/src/
├── components/
│   ├── Chat/
│   ├── Projects/
│   ├── Providers/
│   └── Common/
├── services/
│   ├── WebSocketService.ts
│   ├── ApiService.ts
│   └── AuthService.ts
├── stores/
│   ├── ProjectStore.ts
│   └── ConfigStore.ts
└── views/
    ├── Dashboard.tsx
    ├── Projects.tsx
    └── Settings.tsx
```

## State Management
```typescript
interface AppState {
  user: UserState;
  projects: ProjectState;
  providers: ProviderState;
  chat: ChatState;
  config: ConfigState;
}

// Using Zustand or Redux
const useStore = create<AppState>((set) => ({
  // State implementation
}));
```

## WebSocket Client
```typescript
class WebSocketClient {
  private ws: WebSocket;
  private reconnectTimer: number;
  
  connect(): void {
    this.ws = new WebSocket('wss://localhost:8082');
    this.setupHandlers();
  }
  
  send(message: Message): void {
    if (this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(message));
    }
  }
  
  private setupHandlers(): void {
    this.ws.onmessage = this.handleMessage;
    this.ws.onerror = this.handleError;
    this.ws.onclose = this.handleClose;
  }
}
```

## UI Components
- Material-UI or Ant Design
- Real-time updates via WebSocket
- Markdown rendering
- Code syntax highlighting
- File tree viewer

## Routing
```typescript
const routes = [
  { path: '/', component: Dashboard },
  { path: '/projects', component: Projects },
  { path: '/project/:id', component: ProjectDetail },
  { path: '/providers', component: Providers },
  { path: '/settings', component: Settings }
];
```