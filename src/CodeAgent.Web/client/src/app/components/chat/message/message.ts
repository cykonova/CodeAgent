import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

export interface Message {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp?: Date;
}

@Component({
  selector: 'app-message',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './message.html',
  styleUrl: './message.scss'
})
export class MessageComponent {
  @Input() message!: Message;
  
  getIcon(): string {
    return this.message.role === 'user' ? 'person' : 'assistant';
  }
}