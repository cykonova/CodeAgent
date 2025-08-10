import { Injectable, OnDestroy } from '@angular/core';
import { 
  Subject, 
  Observable, 
  BehaviorSubject, 
  timer, 
  throwError,
  fromEvent,
  EMPTY
} from 'rxjs';
import { 
  retryWhen, 
  tap, 
  delayWhen, 
  filter,
  map,
  takeUntil,
  catchError,
  distinctUntilChanged
} from 'rxjs/operators';
import { 
  MessageEnvelope,
  ServerResponse,
  WebSocketMessageType, 
  WebSocketConfig,
  WebSocketState
} from '../models/websocket.model';

@Injectable({
  providedIn: 'root'
})
export class WebSocketService implements OnDestroy {
  private socket?: WebSocket;
  private config: WebSocketConfig = {
    url: 'ws://localhost:8080/ws',
    reconnect: true,
    reconnectInterval: 5000,
    reconnectAttempts: 5,
    heartbeatInterval: 30000
  };
  
  private messagesSubject = new Subject<ServerResponse>();
  private connectionStateSubject = new BehaviorSubject<WebSocketState>(
    WebSocketState.Disconnected
  );
  private destroy$ = new Subject<void>();
  private messageQueue: MessageEnvelope[] = [];
  private reconnectAttempt = 0;
  private heartbeatTimer?: any;
  
  public messages$ = this.messagesSubject.asObservable();
  public connectionState$ = this.connectionStateSubject.asObservable();
  
  // Computed observables for UI compatibility
  public readonly isConnected$ = this.connectionState$.pipe(
    map(state => state === WebSocketState.Connected),
    distinctUntilChanged()
  );
  
  public readonly isConnecting$ = this.connectionState$.pipe(
    map(state => state === WebSocketState.Connecting),
    distinctUntilChanged()
  );
  
  public readonly connectionIcon$ = this.connectionState$.pipe(
    map(state => {
      switch (state) {
        case WebSocketState.Connected:
          return 'check_circle';
        case WebSocketState.Connecting:
        case WebSocketState.Reconnecting:
          return 'sync';
        case WebSocketState.Disconnected:
          return 'error';
        default:
          return 'help';
      }
    })
  );
  
  public readonly connectionClass$ = this.connectionState$.pipe(
    map(state => `connection-${state.toLowerCase()}`)
  );
  
  public readonly connectionText$ = this.connectionState$.pipe(
    map(state => {
      switch (state) {
        case WebSocketState.Connected:
          return 'Connected';
        case WebSocketState.Connecting:
          return 'Connecting...';
        case WebSocketState.Reconnecting:
          return 'Reconnecting...';
        case WebSocketState.Disconnected:
          return 'Disconnected';
        case WebSocketState.Disconnecting:
          return 'Disconnecting...';
        default:
          return 'Unknown';
      }
    })
  );
  
  constructor() {
    // Auto-connect on service creation
    this.connect();
  }
  
  ngOnDestroy() {
    this.disconnect();
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  connect(url?: string): void {
    if (url) {
      this.config.url = url;
    }
    
    if (this.socket?.readyState === WebSocket.OPEN) {
      console.log('WebSocket already connected');
      return;
    }
    
    this.connectionStateSubject.next(WebSocketState.Connecting);
    
    try {
      this.socket = new WebSocket(this.config.url);
      this.setupSocketListeners();
    } catch (error) {
      console.error('WebSocket connection error:', error);
      this.handleReconnection();
    }
  }
  
  private setupSocketListeners(): void {
    if (!this.socket) return;
    
    this.socket.onopen = (event) => {
      console.log('WebSocket connected');
      this.connectionStateSubject.next(WebSocketState.Connected);
      this.reconnectAttempt = 0;
      this.flushMessageQueue();
      this.startHeartbeat();
    };
    
    this.socket.onmessage = (event) => {
      try {
        const response: ServerResponse = JSON.parse(event.data);
        
        // Handle pong messages internally
        if (response.type === 'pong') {
          return;
        }
        
        this.messagesSubject.next(response);
      } catch (error) {
        console.error('Error parsing WebSocket message:', error);
      }
    };
    
    this.socket.onerror = (error) => {
      console.error('WebSocket error:', error);
    };
    
    this.socket.onclose = (event) => {
      console.log('WebSocket disconnected', event);
      this.connectionStateSubject.next(WebSocketState.Disconnected);
      this.stopHeartbeat();
      
      if (this.config.reconnect && !event.wasClean) {
        this.handleReconnection();
      }
    };
  }
  
  private handleReconnection(): void {
    if (this.reconnectAttempt >= this.config.reconnectAttempts) {
      console.error('Max reconnection attempts reached');
      return;
    }
    
    this.reconnectAttempt++;
    this.connectionStateSubject.next(WebSocketState.Reconnecting);
    
    console.log(`Reconnecting... Attempt ${this.reconnectAttempt}`);
    
    setTimeout(() => {
      this.connect();
    }, this.config.reconnectInterval);
  }
  
  disconnect(): void {
    if (this.socket) {
      this.connectionStateSubject.next(WebSocketState.Disconnecting);
      this.config.reconnect = false;
      this.socket.close();
      this.socket = undefined;
      this.stopHeartbeat();
    }
  }
  
  send<T = any>(type: string, payload?: T, correlationId?: string): void {
    const message: MessageEnvelope<T> = {
      Type: type,
      Payload: payload,
      CorrelationId: correlationId
    };
    
    if (this.socket?.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(message));
    } else {
      // Queue message for later delivery
      this.messageQueue.push(message);
      console.log('Message queued for delivery:', message);
    }
  }
  
  private flushMessageQueue(): void {
    while (this.messageQueue.length > 0) {
      const message = this.messageQueue.shift();
      if (message && this.socket?.readyState === WebSocket.OPEN) {
        this.socket.send(JSON.stringify(message));
      }
    }
  }
  
  private startHeartbeat(): void {
    this.stopHeartbeat();
    
    this.heartbeatTimer = setInterval(() => {
      if (this.socket?.readyState === WebSocket.OPEN) {
        // Match backend's ping format
        const pingMessage = JSON.stringify({ type: 'ping' });
        this.socket.send(pingMessage);
      }
    }, this.config.heartbeatInterval);
  }
  
  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = undefined;
    }
  }
  
  // Observable for specific response types
  on<T extends ServerResponse = ServerResponse>(type: string): Observable<T> {
    return this.messages$.pipe(
      filter(message => message.type === type),
      map(message => message as T)
    );
  }
  
  // Request-response pattern with correlation ID
  request<TRequest, TResponse extends ServerResponse>(
    type: string, 
    payload?: TRequest
  ): Observable<TResponse> {
    const correlationId = this.generateId();
    
    // Send the request with correlation ID
    this.send(type, payload, correlationId);
    
    // Wait for correlated response (backend should echo correlationId)
    return this.messages$.pipe(
      filter(msg => (msg as any).correlationId === correlationId),
      map(msg => msg as TResponse),
      takeUntil(timer(30000)) // 30 second timeout
    );
  }
  
  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
  
  isConnected(): boolean {
    return this.socket?.readyState === WebSocket.OPEN;
  }
  
  getConnectionState(): WebSocketState {
    return this.connectionStateSubject.value;
  }
}