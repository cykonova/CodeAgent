import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';

export interface ToolCall {
  name: string;
  arguments?: any;
  result?: any;
}

@Component({
  selector: 'app-tool-call',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatChipsModule, MatExpansionModule],
  templateUrl: './tool-call.html',
  styleUrl: './tool-call.scss'
})
export class ToolCallComponent {
  @Input() toolCall!: ToolCall;
  
  get isRespondToUser(): boolean {
    return this.toolCall.name === 'respond_to_user';
  }
  
  get toolIcon(): string {
    const iconMap: Record<string, string> = {
      'respond_to_user': 'chat_bubble',
      'search': 'search',
      'read_file': 'description',
      'write_file': 'edit_note',
      'execute_command': 'terminal',
      'browse_web': 'public',
      'analyze_code': 'code',
      'default': 'build'
    };
    
    return iconMap[this.toolCall.name] || iconMap['default'];
  }
  
  getArgumentsDisplay(): string {
    if (!this.toolCall.arguments) return '{}';
    
    try {
      if (typeof this.toolCall.arguments === 'string') {
        return this.toolCall.arguments;
      }
      return JSON.stringify(this.toolCall.arguments, null, 2);
    } catch {
      return String(this.toolCall.arguments);
    }
  }
  
  getMessageContent(): string {
    if (this.isRespondToUser && this.toolCall.arguments?.message) {
      return this.toolCall.arguments.message;
    }
    return '';
  }
}