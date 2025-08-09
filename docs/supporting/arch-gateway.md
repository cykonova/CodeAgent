# Gateway Architecture

## WebSocket Protocol
```typescript
interface Message {
  id: string;
  type: 'command' | 'response' | 'stream' | 'status' | 'error';
  timestamp: string;
  correlationId?: string;
  payload: any;
}
```

## Connection Lifecycle
1. Client connects via WSS
2. Authentication handshake
3. Session establishment
4. Message exchange
5. Heartbeat monitoring
6. Graceful disconnect

## Message Flow
Event-driven architecture using message bus:
```
Client -> Gateway -> Event Bus -> Subscribers
                        |
                        v
                  Event Handlers -> Result Events -> Client
```

## Load Balancing
- Round-robin distribution
- Session affinity
- Health checks
- Automatic failover

## Key Classes
- `WebSocketHandler`
- `ConnectionManager`
- `MessageDispatcher`
- `HeartbeatMonitor`