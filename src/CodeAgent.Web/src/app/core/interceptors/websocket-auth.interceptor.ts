import { Injectable } from '@angular/core';
import { WebSocketService } from '../services/websocket.service';
import { WebSocketMessageType } from '../models/websocket.model';

@Injectable()
export class WebSocketAuthInterceptor {
  constructor(private wsService: WebSocketService) {
    this.setupAuthHandling();
  }
  
  private setupAuthHandling(): void {
    // Listen for connection events
    this.wsService.connectionState$.subscribe(state => {
      if (state === 'CONNECTED') {
        this.authenticate();
      }
    });
  }
  
  private authenticate(): void {
    const token = localStorage.getItem('auth_token');
    if (token) {
      // Send auth message matching backend's expected format
      this.wsService.send('auth', {
        token,
        clientId: this.getClientId()
      });
    }
  }
  
  private getClientId(): string {
    let clientId = localStorage.getItem('client_id');
    if (!clientId) {
      clientId = `client-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
      localStorage.setItem('client_id', clientId);
    }
    return clientId;
  }
}