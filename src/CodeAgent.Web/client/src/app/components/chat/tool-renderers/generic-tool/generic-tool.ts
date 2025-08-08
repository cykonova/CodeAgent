import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { ToolCall } from '../../../../services/chat.service';
import { ToolRenderer } from '../tool-renderer.interface';

@Component({
  selector: 'app-generic-tool',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatExpansionModule],
  template: `
    <div class="generic-tool">
      <div class="tool-header">
        <mat-icon class="tool-icon">build</mat-icon>
        <span class="tool-name">{{ toolCall.name }}</span>
      </div>
      
      <mat-expansion-panel [expanded]="false">
        <mat-expansion-panel-header>
          <mat-panel-title>Parameters</mat-panel-title>
        </mat-expansion-panel-header>
        <div class="parameters">
          <div *ngFor="let param of parameters" class="parameter">
            <span class="param-key">{{ param.key }}:</span>
            <span class="param-value">{{ param.value }}</span>
          </div>
        </div>
      </mat-expansion-panel>
      
      <mat-expansion-panel *ngIf="toolCall.result" [expanded]="false">
        <mat-expansion-panel-header>
          <mat-panel-title>Result</mat-panel-title>
        </mat-expansion-panel-header>
        <pre class="result">{{ formatResult() }}</pre>
      </mat-expansion-panel>
    </div>
  `,
  styles: [`
    .generic-tool {
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
      font-family: 'Roboto Mono', monospace;
    }
    
    .parameters {
      padding: 12px;
    }
    
    .parameter {
      display: flex;
      gap: 8px;
      margin: 4px 0;
      font-size: 0.9em;
    }
    
    .param-key {
      font-weight: 500;
      color: var(--primary);
    }
    
    .param-value {
      font-family: 'Roboto Mono', monospace;
      color: var(--on-surface-variant);
    }
    
    .result {
      background: var(--surface);
      padding: 12px;
      border-radius: 4px;
      overflow-x: auto;
      font-family: 'Roboto Mono', monospace;
      font-size: 0.85em;
      margin: 0;
    }
    
    mat-expansion-panel {
      background: transparent !important;
      box-shadow: none !important;
      margin-top: 8px;
    }
  `]
})
export class GenericToolComponent implements ToolRenderer {
  @Input() toolCall!: ToolCall;
  
  toolName = 'generic';
  
  get parameters(): { key: string, value: string }[] {
    const args = this.toolCall.arguments || this.toolCall.parameters || {};
    return Object.entries(args).map(([key, value]) => ({
      key,
      value: this.formatValue(value)
    }));
  }
  
  private formatValue(value: any): string {
    if (value === null || value === undefined) {
      return 'null';
    }
    if (typeof value === 'object') {
      if (Array.isArray(value)) {
        return `[${value.length} items]`;
      }
      return JSON.stringify(value, null, 2);
    }
    return String(value);
  }
  
  formatResult(): string {
    if (!this.toolCall.result) return '';
    
    if (typeof this.toolCall.result === 'string') {
      return this.toolCall.result;
    }
    
    return JSON.stringify(this.toolCall.result, null, 2);
  }
  
  canRender(toolCall: ToolCall): boolean {
    // Generic renderer can handle any tool
    return true;
  }
  
  handleStream(chunk: string): void {
    if (!this.toolCall.streamContent) {
      this.toolCall.streamContent = '';
    }
    this.toolCall.streamContent += chunk;
  }
}