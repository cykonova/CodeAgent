import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToolCall, ToolRenderer } from '../tool-renderer.interface';

@Component({
  selector: 'app-respond-to-user',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="respond-to-user">
      <div class="message-content" [innerHTML]="formattedContent()"></div>
    </div>
  `,
  styles: [`
    .respond-to-user {
      padding: 0;
    }
    
    .message-content {
      white-space: pre-wrap;
      word-wrap: break-word;
      line-height: 1.6;
    }
    
    .message-content :deep(pre) {
      background: var(--surface-variant);
      border-radius: 4px;
      padding: 12px;
      overflow-x: auto;
      margin: 8px 0;
    }
    
    .message-content :deep(code) {
      background: var(--surface-variant);
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Roboto Mono', monospace;
      font-size: 0.9em;
    }
    
    .message-content :deep(pre code) {
      background: transparent;
      padding: 0;
    }
  `]
})
export class RespondToUserComponent implements ToolRenderer {
  @Input() toolCall!: ToolCall;
  
  toolName = 'respond_to_user';
  content = signal<string>('');
  
  ngOnInit() {
    // Initialize content from arguments
    const message = this.toolCall.arguments?.message || 
                   this.toolCall.parameters?.message || 
                   this.toolCall.streamContent || '';
    this.content.set(message);
  }
  
  canRender(toolCall: ToolCall): boolean {
    return toolCall.name === 'respond_to_user';
  }
  
  handleStream(chunk: string): void {
    this.content.update(current => current + chunk);
  }
  
  onStreamComplete(): void {
    // Any cleanup or final processing
  }
  
  formattedContent = signal<string>('');
  
  ngAfterContentInit() {
    // Format content with basic markdown support
    this.formattedContent.set(this.formatMarkdown(this.content()));
  }
  
  private formatMarkdown(text: string): string {
    // Basic markdown to HTML conversion
    let formatted = text;
    
    // Code blocks
    formatted = formatted.replace(/```(\w+)?\n([\s\S]*?)```/g, 
      '<pre><code class="language-$1">$2</code></pre>');
    
    // Inline code
    formatted = formatted.replace(/`([^`]+)`/g, '<code>$1</code>');
    
    // Bold
    formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    
    // Italic
    formatted = formatted.replace(/\*(.*?)\*/g, '<em>$1</em>');
    
    // Links
    formatted = formatted.replace(/\[([^\]]+)\]\(([^)]+)\)/g, 
      '<a href="$2" target="_blank" rel="noopener">$1</a>');
    
    // Line breaks
    formatted = formatted.replace(/\n/g, '<br>');
    
    return formatted;
  }
}