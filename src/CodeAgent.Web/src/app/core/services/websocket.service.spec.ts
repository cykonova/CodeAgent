import { TestBed } from '@angular/core/testing';
import { WebSocketService } from './websocket.service';
import { WebSocketState } from '../models/websocket.model';

describe('WebSocketService', () => {
  let service: WebSocketService;
  let mockWebSocket: any;
  
  beforeEach(() => {
    TestBed.configureTestingModule({});
    
    // Create mock WebSocket
    mockWebSocket = jasmine.createSpyObj('WebSocket', ['send', 'close']);
    mockWebSocket.readyState = WebSocket.CONNECTING;
    
    // Mock WebSocket constructor
    spyOn(window as any, 'WebSocket').and.returnValue(mockWebSocket as any);
    
    service = TestBed.inject(WebSocketService);
  });
  
  afterEach(() => {
    service.ngOnDestroy();
  });
  
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
  
  it('should attempt to connect on creation', () => {
    expect(window.WebSocket).toHaveBeenCalledWith('ws://localhost:8080/ws');
  });
  
  it('should connect to WebSocket with custom URL', () => {
    service.connect('ws://test.com');
    expect(window.WebSocket).toHaveBeenCalledWith('ws://test.com');
  });
  
  it('should send messages when connected', () => {
    mockWebSocket.readyState = WebSocket.OPEN;
    service.send('ping');
    expect(mockWebSocket.send).toHaveBeenCalled();
  });
  
  it('should queue messages when disconnected', () => {
    mockWebSocket.readyState = WebSocket.CLOSED;
    service.send('ping');
    expect(mockWebSocket.send).not.toHaveBeenCalled();
  });
  
  it('should format message with correct structure', () => {
    mockWebSocket.readyState = WebSocket.OPEN;
    service.send('auth', { token: 'test-token' });
    
    const sentMessage = JSON.parse(mockWebSocket.send.calls.mostRecent().args[0]);
    expect(sentMessage.Type).toBe('auth');
    expect(sentMessage.Payload).toEqual({ token: 'test-token' });
  });
  
  it('should include correlation ID when provided', () => {
    mockWebSocket.readyState = WebSocket.OPEN;
    service.send('command', { cmd: 'test' }, 'corr-123');
    
    const sentMessage = JSON.parse(mockWebSocket.send.calls.mostRecent().args[0]);
    expect(sentMessage.Type).toBe('command');
    expect(sentMessage.Payload).toEqual({ cmd: 'test' });
    expect(sentMessage.CorrelationId).toBe('corr-123');
  });
  
  it('should emit connection state changes', (done) => {
    service.connectionState$.subscribe(state => {
      if (state === WebSocketState.Connecting) {
        done();
      }
    });
  });
  
  it('should handle onopen event', () => {
    let connectionState: WebSocketState | undefined;
    service.connectionState$.subscribe(state => {
      connectionState = state;
    });
    
    // Trigger onopen
    if (mockWebSocket.onopen) {
      mockWebSocket.onopen(new Event('open'));
    }
    
    expect(connectionState).toBe(WebSocketState.Connected);
  });
  
  it('should handle onclose event', () => {
    let connectionState: WebSocketState | undefined;
    service.connectionState$.subscribe(state => {
      connectionState = state;
    });
    
    // Trigger onclose
    if (mockWebSocket.onclose) {
      mockWebSocket.onclose(new CloseEvent('close'));
    }
    
    expect(connectionState).toBe(WebSocketState.Disconnected);
  });
  
  it('should parse and emit incoming messages', (done) => {
    const testMessage = { type: 'test_response', data: 'test' };
    
    service.messages$.subscribe(message => {
      expect(message).toEqual(testMessage);
      done();
    });
    
    // Trigger onmessage
    if (mockWebSocket.onmessage) {
      mockWebSocket.onmessage(new MessageEvent('message', {
        data: JSON.stringify(testMessage)
      }));
    }
  });
  
  it('should not emit pong messages', () => {
    let messageReceived = false;
    const pongMessage = { type: 'pong', timestamp: '2024-01-01' };
    
    service.messages$.subscribe(() => {
      messageReceived = true;
    });
    
    // Trigger onmessage with pong
    if (mockWebSocket.onmessage) {
      mockWebSocket.onmessage(new MessageEvent('message', {
        data: JSON.stringify(pongMessage)
      }));
    }
    
    expect(messageReceived).toBe(false);
  });
  
  it('should filter messages by type using on() method', (done) => {
    const authMessage = { type: 'auth_response', success: true, sessionId: '123' };
    const chatMessage = { type: 'chat_response', message: 'Hello' };
    
    service.on('auth_response').subscribe(message => {
      expect(message.type).toBe('auth_response');
      done();
    });
    
    // Emit chat message first (should be filtered out)
    if (mockWebSocket.onmessage) {
      mockWebSocket.onmessage(new MessageEvent('message', {
        data: JSON.stringify(chatMessage)
      }));
    }
    
    // Emit auth message (should pass through)
    if (mockWebSocket.onmessage) {
      mockWebSocket.onmessage(new MessageEvent('message', {
        data: JSON.stringify(authMessage)
      }));
    }
  });
  
  it('should disconnect properly', () => {
    service.disconnect();
    expect(mockWebSocket.close).toHaveBeenCalled();
  });
  
  it('should return connection status', () => {
    mockWebSocket.readyState = WebSocket.OPEN;
    expect(service.isConnected()).toBe(true);
    
    mockWebSocket.readyState = WebSocket.CLOSED;
    expect(service.isConnected()).toBe(false);
  });
  
  it('should return current connection state', () => {
    expect(service.getConnectionState()).toBe(WebSocketState.Connecting);
    
    // Trigger connection
    if (mockWebSocket.onopen) {
      mockWebSocket.onopen(new Event('open'));
    }
    
    expect(service.getConnectionState()).toBe(WebSocketState.Connected);
  });
  
  it('should flush message queue when connected', () => {
    // Queue messages while disconnected
    mockWebSocket.readyState = WebSocket.CONNECTING;
    service.send('message1', { data: 1 });
    service.send('message2', { data: 2 });
    expect(mockWebSocket.send).not.toHaveBeenCalled();
    
    // Connect and trigger flush
    mockWebSocket.readyState = WebSocket.OPEN;
    if (mockWebSocket.onopen) {
      mockWebSocket.onopen(new Event('open'));
    }
    
    // Should send both queued messages
    expect(mockWebSocket.send).toHaveBeenCalledTimes(2);
  });
  
  it('should generate unique correlation IDs', () => {
    const ids = new Set<string>();
    
    // Generate multiple IDs using request method
    for (let i = 0; i < 10; i++) {
      mockWebSocket.readyState = WebSocket.OPEN;
      service.request('test', { index: i });
      
      const sentMessage = JSON.parse(mockWebSocket.send.calls.mostRecent().args[0]);
      ids.add(sentMessage.CorrelationId);
    }
    
    // All IDs should be unique
    expect(ids.size).toBe(10);
  });
});