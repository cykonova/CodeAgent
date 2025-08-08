import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { ToolCall } from '../../../../services/chat.service';
import { ToolRenderer } from '../tool-renderer.interface';

@Component({
  selector: 'app-read-file',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatExpansionModule],
  template: `
    <div class="read-file-tool">
      <div class="tool-header">
        <mat-icon class="tool-icon">description</mat-icon>
        <span class="tool-name">Read File</span>
        <span class="file-path">{{ filePath }}</span>
      </div>
      
      <div class="tool-info" *ngIf="lineInfo">
        <span class="line-info">{{ lineInfo }}</span>
      </div>
      
      <mat-expansion-panel [expanded]="expanded()">
        <mat-expansion-panel-header>
          <mat-panel-title>
            <mat-icon>code</mat-icon>
            File Content
          </mat-panel-title>
        </mat-expansion-panel-header>
        <pre class="file-content">{{ fileContent }}</pre>
      </mat-expansion-panel>
    </div>
  `,
  styles: [`
    .read-file-tool {
      padding: 12px;
      background: var(--surface-variant);
      border-radius: 8px;
      margin: 8px 0;
    }
    
    .tool-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;
    }
    
    .tool-icon {
      color: var(--primary);
      font-size: 20px;
      width: 20px;
      height: 20px;
    }
    
    .tool-name {
      font-weight: 500;
    }
    
    .file-path {
      font-family: 'Roboto Mono', monospace;
      font-size: 0.9em;
      color: var(--on-surface-variant);
      background: var(--surface);
      padding: 2px 8px;
      border-radius: 4px;
    }
    
    .tool-info {
      margin: 8px 0;
      font-size: 0.9em;
      color: var(--on-surface-variant);
    }
    
    .line-info {
      background: var(--surface);
      padding: 2px 8px;
      border-radius: 4px;
    }
    
    .file-content {
      background: var(--surface);
      padding: 16px;
      border-radius: 4px;
      overflow-x: auto;
      max-height: 400px;
      overflow-y: auto;
      font-family: 'Roboto Mono', monospace;
      font-size: 0.85em;
      line-height: 1.5;
      margin: 0;
    }
    
    mat-expansion-panel {
      background: transparent !important;
      box-shadow: none !important;
    }
  `]
})
export class ReadFileComponent implements ToolRenderer {
  @Input() toolCall!: ToolCall;
  
  toolName = 'read_file';
  expanded = signal(true);
  
  get filePath(): string {
    return this.toolCall.arguments?.path || 
           this.toolCall.arguments?.file_path ||
           this.toolCall.parameters?.path || 
           this.toolCall.parameters?.file_path || 
           'Unknown file';
  }
  
  get fileContent(): string {
    // Check if content is in result or direct in arguments
    if (this.toolCall.result?.content) {
      return this.toolCall.result.content;
    }
    if (typeof this.toolCall.result === 'string') {
      return this.toolCall.result;
    }
    return this.toolCall.streamContent || 'Loading...';
  }
  
  get lineInfo(): string {
    const startLine = this.toolCall.arguments?.start_line || 
                     this.toolCall.parameters?.start_line;
    const endLine = this.toolCall.arguments?.end_line || 
                   this.toolCall.parameters?.end_line;
    
    if (startLine && endLine) {
      return `Lines ${startLine}-${endLine}`;
    } else if (startLine) {
      return `From line ${startLine}`;
    }
    return '';
  }
  
  canRender(toolCall: ToolCall): boolean {
    return toolCall.name === 'read_file' || 
           toolCall.name === 'view_file' ||
           toolCall.name === 'get_file_content';
  }
  
  handleStream(chunk: string): void {
    if (!this.toolCall.streamContent) {
      this.toolCall.streamContent = '';
    }
    this.toolCall.streamContent += chunk;
  }
}