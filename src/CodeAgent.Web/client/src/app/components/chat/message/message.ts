import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { ToolCallComponent } from '../tool-call/tool-call';

export interface ToolCall {
  id?: string;
  name: string;
  parameters?: any;
  arguments?: any;
  result?: any;
}

export interface Message {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  toolCalls?: ToolCall[];
  metadata?: any;
}

@Component({
  selector: 'app-message',
  standalone: true,
  imports: [CommonModule, MatIconModule, ToolCallComponent],
  templateUrl: './message.html',
  styleUrl: './message.scss'
})
export class MessageComponent {
  @Input() message!: Message;
  
  getIcon(): string {
    return this.message.role === 'user' ? 'person' : 'assistant';
  }
}