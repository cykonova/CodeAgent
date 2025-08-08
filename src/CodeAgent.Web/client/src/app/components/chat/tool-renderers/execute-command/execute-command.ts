import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { ToolCall, ToolRenderer } from '../tool-renderer.interface';

@Component({
  selector: 'app-execute-command',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatExpansionModule],
  template: `
    <div class="execute-command-tool">
      <div class="tool-header">
        <mat-icon class="tool-icon">terminal</mat-icon>
        <span class="tool-name">Execute Command</span>
      </div>
      
      <div class="command-line">
        <span class="prompt">$</span>
        <code class="command">{{ command }}</code>
      </div>
      
      <mat-expansion-panel [expanded]="hasOutput" *ngIf="hasOutput">
        <mat-expansion-panel-header>
          <mat-panel-title>
            <mat-icon>{{ exitCode === 0 ? 'check_circle' : 'error' }}</mat-icon>
            Output
            <span class="exit-code" *ngIf="exitCode !== null">
              (exit code: {{ exitCode }})
            </span>
          </mat-panel-title>
        </mat-expansion-panel-header>
        <pre class="command-output">{{ output }}</pre>
      </mat-expansion-panel>
    </div>
  `,
  styles: [`
    .execute-command-tool {
      padding: 12px;
      background: var(--surface-variant);
      border-radius: 8px;
      margin: 8px 0;
    }
    
    .tool-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
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
    
    .command-line {
      background: #1e1e1e;
      color: #d4d4d4;
      padding: 12px;
      border-radius: 4px;
      font-family: 'Roboto Mono', monospace;
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;
    }
    
    .prompt {
      color: #4ec9b0;
      font-weight: bold;
    }
    
    .command {
      flex: 1;
      word-break: break-all;
    }
    
    .command-output {
      background: #1e1e1e;
      color: #d4d4d4;
      padding: 16px;
      border-radius: 4px;
      overflow-x: auto;
      max-height: 400px;
      overflow-y: auto;
      font-family: 'Roboto Mono', monospace;
      font-size: 0.85em;
      line-height: 1.5;
      margin: 0;
      white-space: pre-wrap;
    }
    
    .exit-code {
      font-size: 0.9em;
      color: var(--on-surface-variant);
      margin-left: 8px;
    }
    
    mat-expansion-panel {
      background: transparent !important;
      box-shadow: none !important;
      margin-top: 8px;
    }
  `]
})
export class ExecuteCommandComponent implements ToolRenderer {
  @Input() toolCall!: ToolCall;
  
  toolName = 'execute_command';
  
  get command(): string {
    return this.toolCall.arguments?.command || 
           this.toolCall.parameters?.command || 
           '';
  }
  
  get output(): string {
    if (this.toolCall.result?.output) {
      return this.toolCall.result.output;
    }
    if (this.toolCall.result?.stdout) {
      let out = this.toolCall.result.stdout;
      if (this.toolCall.result.stderr) {
        out += '\n' + this.toolCall.result.stderr;
      }
      return out;
    }
    if (typeof this.toolCall.result === 'string') {
      return this.toolCall.result;
    }
    return this.toolCall.streamContent || '';
  }
  
  get exitCode(): number | null {
    if (this.toolCall.result?.exit_code !== undefined) {
      return this.toolCall.result.exit_code;
    }
    if (this.toolCall.result?.exitCode !== undefined) {
      return this.toolCall.result.exitCode;
    }
    return null;
  }
  
  get hasOutput(): boolean {
    return this.output.length > 0;
  }
  
  canRender(toolCall: ToolCall): boolean {
    return toolCall.name === 'execute_command' || 
           toolCall.name === 'run_command' ||
           toolCall.name === 'shell_execute' ||
           toolCall.name === 'bash';
  }
  
  handleStream(chunk: string): void {
    if (!this.toolCall.streamContent) {
      this.toolCall.streamContent = '';
    }
    this.toolCall.streamContent += chunk;
  }
}