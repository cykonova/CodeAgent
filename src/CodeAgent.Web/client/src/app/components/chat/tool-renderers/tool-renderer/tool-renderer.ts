import { Component, Input, ViewChild, ViewContainerRef, ComponentRef, OnInit, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToolCall } from '../tool-renderer.interface';

// Import all tool renderers
import { RespondToUserComponent } from '../respond-to-user/respond-to-user';
import { WriteFileComponent } from '../write-file/write-file';
import { ReadFileComponent } from '../read-file/read-file';
import { ExecuteCommandComponent } from '../execute-command/execute-command';
import { GenericToolComponent } from '../generic-tool/generic-tool';

@Component({
  selector: 'app-tool-renderer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="tool-renderer">
      <ng-container #dynamicRenderer></ng-container>
    </div>
  `,
  styles: [`
    .tool-renderer {
      width: 100%;
    }
  `]
})
export class ToolRendererComponent implements OnInit, OnChanges {
  @Input() toolCall!: ToolCall;
  @ViewChild('dynamicRenderer', { read: ViewContainerRef, static: true }) 
  dynamicRenderer!: ViewContainerRef;
  
  private componentRef?: ComponentRef<any>;
  
  // Map tool names to their renderer components
  private toolRenderers = new Map<string, any>([
    ['respond_to_user', RespondToUserComponent],
    ['write_file', WriteFileComponent],
    ['create_file', WriteFileComponent],
    ['update_file', WriteFileComponent],
    ['read_file', ReadFileComponent],
    ['view_file', ReadFileComponent],
    ['get_file_content', ReadFileComponent],
    ['execute_command', ExecuteCommandComponent],
    ['run_command', ExecuteCommandComponent],
    ['shell_execute', ExecuteCommandComponent],
    ['bash', ExecuteCommandComponent],
  ]);
  
  ngOnInit() {
    this.loadRenderer();
  }
  
  ngOnChanges() {
    if (this.componentRef) {
      // Update the existing component's input
      this.componentRef.instance.toolCall = this.toolCall;
      
      // If it has a handleStream method and we have streaming content
      if (this.toolCall.isStreaming && this.componentRef.instance.handleStream) {
        this.componentRef.instance.handleStream(this.toolCall.streamContent || '');
      }
    }
  }
  
  private loadRenderer() {
    // Clear any existing component
    this.dynamicRenderer.clear();
    
    // Select the appropriate renderer
    const rendererComponent = this.getRendererForTool(this.toolCall.name);
    
    // Create the component
    this.componentRef = this.dynamicRenderer.createComponent(rendererComponent);
    
    // Set the input
    this.componentRef.instance.toolCall = this.toolCall;
  }
  
  private getRendererForTool(toolName: string): any {
    // Check for exact match
    if (this.toolRenderers.has(toolName)) {
      return this.toolRenderers.get(toolName);
    }
    
    // Check for pattern matches
    const lowerName = toolName.toLowerCase();
    
    if (lowerName.includes('write') || lowerName.includes('create') || lowerName.includes('save')) {
      return WriteFileComponent;
    }
    
    if (lowerName.includes('read') || lowerName.includes('view') || lowerName.includes('get')) {
      return ReadFileComponent;
    }
    
    if (lowerName.includes('execute') || lowerName.includes('run') || lowerName.includes('command')) {
      return ExecuteCommandComponent;
    }
    
    if (lowerName.includes('respond') || lowerName.includes('message') || lowerName.includes('say')) {
      return RespondToUserComponent;
    }
    
    // Default to generic renderer
    return GenericToolComponent;
  }
  
  // Handle streaming updates
  handleStreamUpdate(chunk: string) {
    if (this.componentRef && this.componentRef.instance.handleStream) {
      this.componentRef.instance.handleStream(chunk);
    }
  }
  
  // Signal stream completion
  onStreamComplete() {
    if (this.componentRef && this.componentRef.instance.onStreamComplete) {
      this.componentRef.instance.onStreamComplete();
    }
  }
}