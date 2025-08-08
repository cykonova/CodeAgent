import { Injectable, signal, OnDestroy } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export interface Message {
  id: string;
  content: string;
  role: 'user' | 'assistant' | 'system';
  timestamp: Date;
  toolCalls?: ToolCall[];
  metadata?: any;
}

export interface ToolCall {
  id?: string;
  name: string;
  parameters?: any;
  arguments?: any;
  result?: any;
  isStreaming?: boolean;
  streamContent?: string;
}

export interface ChatRequest {
  message: string;
  contextPath?: string;
  provider?: string;
  model?: string;
  temperature?: number;
  maxTokens?: number;
  stream?: boolean;
}

export interface ChatResponse {
  id: string;
  content: string;
  role: string;
  toolCalls?: ToolCall[];
  usage?: {
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
  };
}

@Injectable({
  providedIn: 'root'
})
export class ChatService implements OnDestroy {
  private apiUrl = `${environment.apiUrl}/api`;
  private messages = signal<Message[]>([]);
  private hubConnection?: signalR.HubConnection;
  private connectionState = signal<'disconnected' | 'connecting' | 'connected'>('disconnected');
  
  constructor(private http: HttpClient) {
    this.initializeSignalR();
    this.loadFromLocalStorage();
    
    // Load session from server on startup
    this.loadSession().subscribe({
      error: (error) => {
        console.warn('Failed to load session from server:', error);
      }
    });
    
    // Start auto-save
    this.startAutoSave();
  }
  
  private async initializeSignalR(): Promise<void> {
    try {
      this.connectionState.set('connecting');
      
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${environment.apiUrl}/hub/agent`)
        .withAutomaticReconnect()
        .build();

      // Set up event handlers
      this.hubConnection.on('Connected', (data: any) => {
        console.log('Connected to SignalR hub:', data);
        this.connectionState.set('connected');
      });

      this.hubConnection.on('MessageReceived', (data: any) => {
        console.log('Message received:', data);
      });

      this.hubConnection.on('StreamChunk', (data: { sessionId: string; chunk: string }) => {
        this.handleStreamChunk(data);
      });

      this.hubConnection.on('MessageResponse', (data: { sessionId: string; content: string; timestamp: string }) => {
        this.handleMessageResponse(data);
      });

      this.hubConnection.on('Error', (error: string) => {
        console.error('Hub error:', error);
        this.handleError(error);
      });

      this.hubConnection.onclose(() => {
        this.connectionState.set('disconnected');
      });

      this.hubConnection.onreconnected(() => {
        this.connectionState.set('connected');
      });

      await this.hubConnection.start();
    } catch (error) {
      console.error('Failed to connect to SignalR hub:', error);
      this.connectionState.set('disconnected');
    }
  }
  
  private handleStreamChunk(data: { sessionId: string; chunk: string }): void {
    // Find the last assistant message and update it
    this.messages.update(msgs => {
      const lastMsg = msgs[msgs.length - 1];
      if (lastMsg && lastMsg.role === 'assistant') {
        return msgs.map((msg, index) => 
          index === msgs.length - 1 
            ? { ...msg, content: msg.content + data.chunk }
            : msg
        );
      } else {
        // Create new streaming message
        const newMsg: Message = {
          id: crypto.randomUUID(),
          content: data.chunk,
          role: 'assistant',
          timestamp: new Date()
        };
        return [...msgs, newMsg];
      }
    });
  }

  private handleMessageResponse(data: { sessionId: string; content: string; timestamp: string }): void {
    const message: Message = {
      id: crypto.randomUUID(),
      content: data.content,
      role: 'assistant',
      timestamp: new Date(data.timestamp)
    };
    this.addMessage(message);
  }

  private handleError(error: string): void {
    const errorMessage: Message = {
      id: crypto.randomUUID(),
      content: `Error: ${error}`,
      role: 'system',
      timestamp: new Date()
    };
    this.addMessage(errorMessage);
  }
  
  getMessages() {
    return this.messages;
  }

  getConnectionState() {
    return this.connectionState;
  }
  
  async sendMessageViaHub(message: string, sessionId?: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('SendMessage', message, sessionId);
    } else {
      throw new Error('Not connected to hub');
    }
  }
  
  sendMessage(request: ChatRequest): Observable<ChatResponse> {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    
    return this.http.post<ChatResponse>(
      `${this.apiUrl}/chat/send`,
      request,
      { headers }
    );
  }
  
  streamMessage(request: ChatRequest): Observable<string> {
    const eventSource = new EventSource(
      `${this.apiUrl}/chat/stream?message=${encodeURIComponent(request.message)}&provider=${request.provider || ''}`
    );
    
    const stream = new Subject<string>();
    
    eventSource.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data);
        stream.next(data.content);
      } catch (e) {
        console.error('Failed to parse SSE data:', e);
      }
    };
    
    eventSource.onerror = (error) => {
      console.error('EventSource error:', error);
      eventSource.close();
      stream.error(error);
    };
    
    eventSource.addEventListener('done', () => {
      eventSource.close();
      stream.complete();
    });

    eventSource.addEventListener('error', (event: any) => {
      const data = JSON.parse(event.data);
      stream.error(new Error(data.error || 'Stream error'));
    });
    
    return stream.asObservable();
  }
  
  addMessage(message: Message): void {
    this.messages.update(msgs => [...msgs, message]);
    this.saveToLocalStorage();
  }
  
  clearMessages(): Observable<any> {
    // Call backend to clear server-side history
    return this.http.post<any>(`${this.apiUrl}/chat/reset`, {}).pipe(
      tap(() => {
        // Clear local state after successful server clear
        this.messages.set([]);
        this.clearLocalStorage();
      })
    );
  }
  
  private saveToLocalStorage(): void {
    try {
      const messagesData = this.messages().map(msg => ({
        ...msg,
        timestamp: msg.timestamp.toISOString()
      }));
      localStorage.setItem('codeagent_messages', JSON.stringify(messagesData));
    } catch (error) {
      console.warn('Failed to save messages to localStorage:', error);
    }
  }

  private clearLocalStorage(): void {
    localStorage.removeItem('codeagent_messages');
  }

  loadFromLocalStorage(): void {
    try {
      const stored = localStorage.getItem('codeagent_messages');
      if (stored) {
        const messagesData = JSON.parse(stored);
        const messages: Message[] = messagesData.map((msg: any) => ({
          ...msg,
          timestamp: new Date(msg.timestamp)
        }));
        this.messages.set(messages);
      }
    } catch (error) {
      console.warn('Failed to load messages from localStorage:', error);
    }
  }
  
  // Get available providers
  getProviders(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/configuration/providers`);
  }
  
  // Get current configuration
  getConfiguration(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/configuration`);
  }
  
  // Update configuration
  updateConfiguration(config: any): Observable<any> {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    return this.http.post<any>(`${this.apiUrl}/configuration`, config, { headers });
  }
  
  // Clear history (alias for clearMessages for compatibility)
  clearHistory(): Observable<any> {
    return this.clearMessages();
  }

  // Session management
  saveSession(): Observable<any> {
    const sessionData = {
      messages: this.messages().map(msg => ({
        ...msg,
        timestamp: msg.timestamp.toISOString()
      })),
      permissions: {} // Add permissions when needed
    };

    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post<any>(`${this.apiUrl}/chat/save-session`, sessionData, { headers });
  }

  loadSession(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/chat/load-session`).pipe(
      tap((session) => {
        if (session.messages && session.messages.length > 0) {
          const messages: Message[] = session.messages.map((msg: any) => ({
            ...msg,
            timestamp: new Date(msg.timestamp)
          }));
          this.messages.set(messages);
        }
      })
    );
  }

  // Auto-save session periodically
  private startAutoSave(): void {
    setInterval(() => {
      if (this.messages().length > 0) {
        this.saveSession().subscribe({
          error: (error) => {
            console.warn('Auto-save failed:', error);
          }
        });
      }
    }, 30000); // Auto-save every 30 seconds
  }

  // Reset connection
  async reconnect(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
    }
    await this.initializeSignalR();
  }

  ngOnDestroy(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }
}