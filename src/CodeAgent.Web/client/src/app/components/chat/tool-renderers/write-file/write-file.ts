import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { ToolCall } from '../../../../services/chat.service';
import { ToolRenderer } from '../tool-renderer.interface';

@Component({
  selector: 'app-write-file',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatExpansionModule],
  template: `
    <div class="write-file-tool">
      <div class="tool-header">
        <mat-icon class="tool-icon">edit_note</mat-icon>
        <span class="tool-name">Write File</span>
        <span class="file-path">{{ filePath }}</span>
      </div>
      
      <div class="tool-actions">
        <button mat-button (click)="toggleContent()">
          <mat-icon>{{ showContent() ? 'visibility_off' : 'visibility' }}</mat-icon>
          {{ showContent() ? 'Hide' : 'View' }} Content
        </button>
        <button mat-button *ngIf="result">
          <mat-icon>check_circle</mat-icon>
          Written Successfully
        </button>
      </div>
      
      <mat-expansion-panel *ngIf="showContent()" [expanded]="showContent()">
        <mat-expansion-panel-header>
          <mat-panel-title>File Content</mat-panel-title>
        </mat-expansion-panel-header>
        <pre class="file-content">{{ fileContent }}</pre>
      </mat-expansion-panel>
    </div>
  `,
  styles: [`
    .write-file-tool {
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
    
    .tool-actions {
      display: flex;
      gap: 8px;
      margin-top: 8px;
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
      margin-top: 12px;
      background: transparent !important;
      box-shadow: none !important;
    }
  `]
})
export class WriteFileComponent implements ToolRenderer {
  @Input() toolCall!: ToolCall;
  
  toolName = 'write_file';
  showContent = signal(false);
  
  get filePath(): string {
    return this.toolCall.arguments?.path || 
           this.toolCall.arguments?.file_path ||
           this.toolCall.parameters?.path || 
           this.toolCall.parameters?.file_path || 
           'Unknown file';
  }
  
  get fileContent(): string {
    return this.toolCall.arguments?.content || 
           this.toolCall.parameters?.content || 
           this.toolCall.streamContent || 
           '';
  }
  
  get result(): any {
    return this.toolCall.result;
  }
  
  canRender(toolCall: ToolCall): boolean {
    return toolCall.name === 'write_file' || 
           toolCall.name === 'create_file' ||
           toolCall.name === 'update_file';
  }
  
  toggleContent(): void {
    this.showContent.update(v => !v);
  }
  
  handleStream(chunk: string): void {
    // Handle streaming content updates if needed
    if (!this.toolCall.streamContent) {
      this.toolCall.streamContent = '';
    }
    this.toolCall.streamContent += chunk;
  }
}