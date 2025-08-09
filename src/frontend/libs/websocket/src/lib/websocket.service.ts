import { Injectable } from '@angular/core';
import { Observable, Subject, BehaviorSubject } from 'rxjs';

export interface WebSocketMessage {
  id: string;
  type: string;
  payload: any;
  timestamp: Date;
}

export interface ConnectionState {
  isConnected: boolean;
  error?: string;
}

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private socket: WebSocket | null = null;
  private messagesSubject = new Subject<WebSocketMessage>();
  private connectionStateSubject = new BehaviorSubject<ConnectionState>({ isConnected: false });
  private reconnectTimeout: any;
  private messageQueue: WebSocketMessage[] = [];

  public messages$ = this.messagesSubject.asObservable();
  public connectionState$ = this.connectionStateSubject.asObservable();

  connect(url: string): void {
    if (this.socket?.readyState === WebSocket.OPEN) {
      return;
    }

    this.socket = new WebSocket(url);

    this.socket.onopen = () => {
      this.connectionStateSubject.next({ isConnected: true });
      this.flushMessageQueue();
    };

    this.socket.onmessage = (event) => {
      const message = JSON.parse(event.data) as WebSocketMessage;
      this.messagesSubject.next(message);
    };

    this.socket.onerror = (error) => {
      this.connectionStateSubject.next({ isConnected: false, error: 'Connection error' });
    };

    this.socket.onclose = () => {
      this.connectionStateSubject.next({ isConnected: false });
      this.scheduleReconnect(url);
    };
  }

  send(message: WebSocketMessage): void {
    if (this.socket?.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(message));
    } else {
      this.messageQueue.push(message);
    }
  }

  disconnect(): void {
    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
    }
    if (this.socket) {
      this.socket.close();
      this.socket = null;
    }
  }

  private flushMessageQueue(): void {
    while (this.messageQueue.length > 0 && this.socket?.readyState === WebSocket.OPEN) {
      const message = this.messageQueue.shift();
      if (message) {
        this.socket.send(JSON.stringify(message));
      }
    }
  }

  private scheduleReconnect(url: string): void {
    this.reconnectTimeout = setTimeout(() => {
      this.connect(url);
    }, 3000);
  }
}