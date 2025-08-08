import { Observable } from 'rxjs';

export interface ToolCall {
  id?: string;
  name: string;
  arguments?: any;
  parameters?: any;
  result?: any;
  isStreaming?: boolean;
  streamContent?: string;
}

export interface ToolRenderer {
  toolName: string;
  canRender(toolCall: ToolCall): boolean;
  handleStream?(chunk: string): void;
  onStreamComplete?(): void;
}