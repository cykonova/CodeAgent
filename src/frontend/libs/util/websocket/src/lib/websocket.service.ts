import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

export interface WebSocketMessage {
  type: string;
  data: any;
  timestamp?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private socket?: WebSocket;
  private messageSubject = new Subject<WebSocketMessage>();
  private connectionSubject = new Subject<boolean>();
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000;
  
  public messages$ = this.messageSubject.asObservable();
  public connection$ = this.connectionSubject.asObservable();

  connect(url: string = 'ws://localhost:5000/ws'): void {
    if (this.socket) {
      this.disconnect();
    }

    this.socket = new WebSocket(url);

    this.socket.onopen = () => {
      console.log('WebSocket connected');
      this.connectionSubject.next(true);
      this.reconnectAttempts = 0;
      
      // Send authentication if needed
      this.send({
        type: 'auth',
        data: { token: this.getAuthToken() }
      });
    };

    this.socket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data);
        this.messageSubject.next(message);
        this.handleMessage(message);
      } catch (error) {
        console.error('Failed to parse WebSocket message:', error);
      }
    };

    this.socket.onerror = (error) => {
      console.error('WebSocket error:', error);
    };

    this.socket.onclose = () => {
      console.log('WebSocket disconnected');
      this.connectionSubject.next(false);
      this.attemptReconnect(url);
    };
  }

  private handleMessage(message: WebSocketMessage): void {
    switch (message.type) {
      case 'agent.status':
        console.log('Agent status update:', message.data);
        break;
      case 'execution.progress':
        console.log('Execution progress:', message.data);
        break;
      case 'project.update':
        console.log('Project update:', message.data);
        break;
      case 'error':
        console.error('WebSocket error message:', message.data);
        break;
      default:
        console.log('Received message:', message);
    }
  }

  private attemptReconnect(url: string): void {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      console.log(`Attempting to reconnect (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
      
      setTimeout(() => {
        this.connect(url);
      }, this.reconnectDelay * this.reconnectAttempts);
    } else {
      console.error('Max reconnection attempts reached');
    }
  }

  private getAuthToken(): string | null {
    return localStorage.getItem('authToken');
  }

  send(message: WebSocketMessage): void {
    if (this.socket && this.socket.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(message));
    } else {
      console.warn('WebSocket is not connected');
    }
  }

  sendCommand(command: string, data: any): void {
    this.send({
      type: 'command',
      data: { command, ...data },
      timestamp: new Date()
    });
  }

  subscribeToAgent(agentId: string): void {
    this.send({
      type: 'subscribe',
      data: { entity: 'agent', id: agentId }
    });
  }

  subscribeToProject(projectId: string): void {
    this.send({
      type: 'subscribe',
      data: { entity: 'project', id: projectId }
    });
  }

  disconnect(): void {
    if (this.socket) {
      this.socket.close();
      this.socket = undefined;
    }
  }
}