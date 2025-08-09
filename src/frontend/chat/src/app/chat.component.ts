import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { ChatService, ChatMessage, ChatSession } from '../../../libs/data-access/src/lib/chat.service';
import { ProgressIndicatorComponent, LoadingOverlayComponent } from '@src/ui-components';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatListModule,
    MatFormFieldModule,
    MatSelectModule,
    MatChipsModule,
    ProgressIndicatorComponent,
    LoadingOverlayComponent
  ],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  @ViewChild('messageInput') private messageInput!: ElementRef;

  currentSession: ChatSession | null = null;
  messages: ChatMessage[] = [];
  inputMessage = '';
  isLoading = false;
  selectedAgent = '';
  selectedProvider = '';

  availableAgents = [
    { id: 'general', name: 'General Assistant' },
    { id: 'code', name: 'Code Assistant' },
    { id: 'review', name: 'Code Reviewer' },
    { id: 'architect', name: 'System Architect' }
  ];

  availableProviders = [
    { id: 'anthropic', name: 'Anthropic Claude' },
    { id: 'openai', name: 'OpenAI GPT' },
    { id: 'ollama', name: 'Ollama (Local)' }
  ];

  constructor(private chatService: ChatService) {}

  ngOnInit(): void {
    this.initializeChat();
    this.subscribeToMessages();
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  private initializeChat(): void {
    const wsUrl = this.getWebSocketUrl();
    this.chatService.connectToGateway(wsUrl);
    this.currentSession = this.chatService.createSession('New Chat Session');
  }

  private getWebSocketUrl(): string {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const host = window.location.hostname;
    const port = '7000';
    return `${protocol}//${host}:${port}/ws`;
  }

  private subscribeToMessages(): void {
    this.chatService.currentSession$.subscribe(session => {
      if (session) {
        this.currentSession = session;
        this.messages = session.messages;
      }
    });

    this.chatService.agentResponses$.subscribe(response => {
      if (response.error) {
        this.handleError(response.error);
      }
      this.isLoading = !response.isComplete;
    });
  }

  sendMessage(): void {
    if (!this.inputMessage.trim()) {
      return;
    }

    this.isLoading = true;
    this.chatService.sendMessage(
      this.inputMessage,
      this.selectedAgent || undefined,
      this.selectedProvider || undefined
    );
    this.inputMessage = '';
    this.focusMessageInput();
  }

  handleEnterKey(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    if (!keyboardEvent.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  clearChat(): void {
    this.chatService.clearSession();
    this.messages = [];
  }

  newSession(): void {
    this.currentSession = this.chatService.createSession('New Chat Session');
    this.messages = [];
  }

  private scrollToBottom(): void {
    try {
      this.messagesContainer.nativeElement.scrollTop = 
        this.messagesContainer.nativeElement.scrollHeight;
    } catch(err) {}
  }

  private focusMessageInput(): void {
    setTimeout(() => {
      this.messageInput.nativeElement.focus();
    }, 0);
  }

  private handleError(error: string): void {
    console.error('Chat error:', error);
  }

  getMessageClass(role: string): string {
    return `message-${role}`;
  }

  formatTimestamp(timestamp: Date): string {
    return new Date(timestamp).toLocaleTimeString();
  }
}