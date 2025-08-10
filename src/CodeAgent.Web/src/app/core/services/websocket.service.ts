import { Injectable, signal, computed } from '@angular/core';
import { Observable, BehaviorSubject, timer, of } from 'rxjs';
import { map, switchMap, distinctUntilChanged } from 'rxjs/operators';

export type ConnectionState = 'connected' | 'connecting' | 'disconnected' | 'error';

export interface WebSocketConfig {
  url: string;
  reconnectInterval?: number;
  reconnectAttempts?: number;
}

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private socket: WebSocket | null = null;
  private connectionState$ = new BehaviorSubject<ConnectionState>('disconnected');
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectInterval = 3000;
  
  // Public observables
  public readonly connectionState = this.connectionState$.asObservable();
  
  // Computed observables for UI
  public readonly isConnected$ = this.connectionState.pipe(
    map(state => state === 'connected'),
    distinctUntilChanged()
  );
  
  public readonly isConnecting$ = this.connectionState.pipe(
    map(state => state === 'connecting'),
    distinctUntilChanged()
  );
  
  public readonly connectionIcon$ = this.connectionState.pipe(
    map(state => {
      switch (state) {
        case 'connected':
          return 'check_circle';
        case 'connecting':
          return 'sync';
        case 'disconnected':
          return 'error';
        case 'error':
          return 'warning';
        default:
          return 'help';
      }
    })
  );
  
  public readonly connectionClass$ = this.connectionState.pipe(
    map(state => `connection-${state}`)
  );
  
  public readonly connectionText$ = this.connectionState.pipe(
    map(state => {
      switch (state) {
        case 'connected':
          return 'Connected';
        case 'connecting':
          return 'Connecting...';
        case 'disconnected':
          return 'Disconnected';
        case 'error':
          return 'Connection Error';
        default:
          return 'Unknown';
      }
    })
  );
  
  constructor() {
    // Auto-connect on service initialization
    // In a real app, this would use actual WebSocket URL from config
    this.simulateConnection();
  }
  
  /**
   * Connect to WebSocket server
   */
  connect(url?: string): void {
    // For now, simulate connection
    this.simulateConnection();
  }
  
  /**
   * Disconnect from WebSocket server
   */
  disconnect(): void {
    if (this.socket) {
      this.socket.close();
      this.socket = null;
    }
    this.connectionState$.next('disconnected');
  }
  
  /**
   * Send message through WebSocket
   */
  send(message: any): void {
    if (this.socket && this.socket.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(message));
    }
  }
  
  /**
   * Simulate connection for demo purposes
   * In production, this would be replaced with actual WebSocket connection
   */
  private simulateConnection(): void {
    this.connectionState$.next('connecting');
    
    // Simulate connection delay
    timer(1500).subscribe(() => {
      this.connectionState$.next('connected');
    });
  }
  
  /**
   * Handle reconnection logic
   */
  private reconnect(): void {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      this.connectionState$.next('connecting');
      
      timer(this.reconnectInterval).subscribe(() => {
        this.connect();
      });
    } else {
      this.connectionState$.next('error');
    }
  }
  
  /**
   * Reset reconnection attempts
   */
  private resetReconnectAttempts(): void {
    this.reconnectAttempts = 0;
  }
}