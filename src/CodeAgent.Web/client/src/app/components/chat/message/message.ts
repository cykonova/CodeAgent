import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { ToolRendererComponent } from '../tool-renderers/tool-renderer/tool-renderer';
import { ToolParserService } from '../../../services/tool-parser.service';
import { ToolCall } from '../../../services/chat.service';

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
  imports: [CommonModule, MatIconModule, ToolRendererComponent],
  templateUrl: './message.html',
  styleUrl: './message.scss'
})
export class MessageComponent {
  @Input() message!: Message;
  
  private toolParser = inject(ToolParserService);
  
  getIcon(): string {
    return this.message.role === 'user' ? 'person' : 'assistant';
  }
  
  getParsedToolCalls(): ToolCall[] {
    // If message already has tool calls, return them
    if (this.message.toolCalls && this.message.toolCalls.length > 0) {
      return this.message.toolCalls;
    }
    
    // Parse content for tool calls
    if (this.message.content && this.message.role === 'assistant') {
      const parsed = this.toolParser.parseMessage(this.message.content);
      return parsed.toolCalls;
    }
    
    return [];
  }
}