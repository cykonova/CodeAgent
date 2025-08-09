import { Injectable } from '@angular/core';
import { Observable, Subject, BehaviorSubject } from 'rxjs';
import { filter, map } from 'rxjs/operators';
import { WebSocketService, WebSocketMessage } from '../../../websocket/src/lib/websocket.service';

export interface ChatMessage {
  id: string;
  content: string;
  role: 'user' | 'assistant' | 'system';
  timestamp: Date;
  agentId?: string;
  providerId?: string;
  metadata?: any;
}

export interface ChatSession {
  id: string;
  title: string;
  messages: ChatMessage[];
  createdAt: Date;
  updatedAt: Date;
  projectId?: string;
}

export interface AgentResponse {
  messageId: string;
  content: string;
  isComplete: boolean;
  error?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private currentSessionSubject = new BehaviorSubject<ChatSession | null>(null);
  private messagesSubject = new Subject<ChatMessage>();
  private agentResponseSubject = new Subject<AgentResponse>();

  public currentSession$ = this.currentSessionSubject.asObservable();
  public messages$ = this.messagesSubject.asObservable();
  public agentResponses$ = this.agentResponseSubject.asObservable();

  constructor(private webSocketService: WebSocketService) {
    this.setupWebSocketListeners();
  }

  private setupWebSocketListeners(): void {
    this.webSocketService.messages$
      .pipe(filter(msg => msg.type === 'chat.message'))
      .subscribe(msg => {
        const chatMessage = this.mapToChatMessage(msg.payload);
        this.messagesSubject.next(chatMessage);
        this.addMessageToCurrentSession(chatMessage);
      });

    this.webSocketService.messages$
      .pipe(filter(msg => msg.type === 'agent.response'))
      .subscribe(msg => {
        this.agentResponseSubject.next(msg.payload as AgentResponse);
      });
  }

  connectToGateway(url: string): void {
    this.webSocketService.connect(url);
  }

  createSession(title: string, projectId?: string): ChatSession {
    const session: ChatSession = {
      id: this.generateId(),
      title,
      messages: [],
      createdAt: new Date(),
      updatedAt: new Date(),
      projectId
    };
    this.currentSessionSubject.next(session);
    return session;
  }

  sendMessage(content: string, agentId?: string, providerId?: string): void {
    const message: ChatMessage = {
      id: this.generateId(),
      content,
      role: 'user',
      timestamp: new Date(),
      agentId,
      providerId
    };

    this.messagesSubject.next(message);
    this.addMessageToCurrentSession(message);

    const wsMessage: WebSocketMessage = {
      id: message.id,
      type: 'chat.send',
      payload: message,
      timestamp: new Date()
    };

    this.webSocketService.send(wsMessage);
  }

  clearSession(): void {
    const currentSession = this.currentSessionSubject.value;
    if (currentSession) {
      currentSession.messages = [];
      currentSession.updatedAt = new Date();
      this.currentSessionSubject.next(currentSession);
    }
  }

  private addMessageToCurrentSession(message: ChatMessage): void {
    const currentSession = this.currentSessionSubject.value;
    if (currentSession) {
      currentSession.messages.push(message);
      currentSession.updatedAt = new Date();
      this.currentSessionSubject.next(currentSession);
    }
  }

  private mapToChatMessage(payload: any): ChatMessage {
    return {
      id: payload.id || this.generateId(),
      content: payload.content,
      role: payload.role,
      timestamp: new Date(payload.timestamp),
      agentId: payload.agentId,
      providerId: payload.providerId,
      metadata: payload.metadata
    };
  }

  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
}