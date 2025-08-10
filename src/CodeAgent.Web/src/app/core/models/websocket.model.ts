export enum WebSocketMessageType {
  // Core message types (matching backend)
  Auth = 'auth',
  Chat = 'chat',
  Command = 'command',
  Ping = 'ping',
  Pong = 'pong',
  Error = 'error',
  
  // Response types
  AuthResponse = 'auth_response',
  ChatResponse = 'chat_response',
  CommandResponse = 'command_response'
}

// Match backend's MessageEnvelope structure
export interface MessageEnvelope<T = any> {
  Type: string;  // Capital case to match backend C# conventions
  Payload?: T;   // JsonElement in backend, generic type in frontend
  CorrelationId?: string;
}

// For typed responses from backend
export interface ServerResponse {
  type: string;
  [key: string]: any;  // Additional properties vary by response type
}

// Specific response types
export interface AuthResponse {
  type: 'auth_response';
  success: boolean;
  sessionId: string;
}

export interface ErrorResponse {
  type: 'error';
  message: string;
}

export interface PingResponse {
  type: 'pong';
  timestamp: string;
}

export interface WebSocketConfig {
  url: string;
  reconnect: boolean;
  reconnectInterval: number;
  reconnectAttempts: number;
  heartbeatInterval: number;
}

export enum WebSocketState {
  Connecting = 'CONNECTING',
  Connected = 'CONNECTED',
  Disconnecting = 'DISCONNECTING',
  Disconnected = 'DISCONNECTED',
  Reconnecting = 'RECONNECTING'
}