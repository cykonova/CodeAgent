import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';

export interface ToolCall {
  name: string;
  arguments?: any;
  parameters?: any;
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
  
  // Expose Object.keys to template
  objectKeys = Object.keys;
  
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
  
  getParametersList(): { key: string, value: string }[] {
    if (!this.toolCall.arguments) return [];
    
    const params: { key: string, value: string }[] = [];
    
    // Handle both arguments and parameters properties
    const args = this.toolCall.arguments || this.toolCall.parameters;
    
    if (args && typeof args === 'object') {
      for (const [key, value] of Object.entries(args)) {
        let displayValue = '';
        
        if (value === null || value === undefined) {
          displayValue = 'null';
        } else if (typeof value === 'object') {
          // For objects/arrays, show a brief summary
          if (Array.isArray(value)) {
            displayValue = `[${value.length} items]`;
          } else {
            displayValue = `{${Object.keys(value).length} properties}`;
          }
        } else {
          displayValue = String(value);
        }
        
        params.push({ key, value: displayValue });
      }
    }
    
    return params;
  }
  
  formatResult(): string {
    if (!this.toolCall.result) return '';
    
    if (typeof this.toolCall.result === 'string') {
      return this.toolCall.result;
    }
    
    // If it's an object with a message or content property, show that
    if (this.toolCall.result.message) {
      return this.toolCall.result.message;
    }
    if (this.toolCall.result.content) {
      return this.toolCall.result.content;
    }
    
    // Otherwise show a summary
    if (typeof this.toolCall.result === 'object') {
      return `Completed successfully`;
    }
    
    return String(this.toolCall.result);
  }
  
  getMessageContent(): string {
    // Extract message from respond_to_user arguments
    if (this.toolCall.arguments?.message) {
      return this.toolCall.arguments.message;
    }
    if (this.toolCall.parameters?.message) {
      return this.toolCall.parameters.message;
    }
    return '';
  }
}