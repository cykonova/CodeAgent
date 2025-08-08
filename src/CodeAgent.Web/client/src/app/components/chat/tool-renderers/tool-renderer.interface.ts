import { Observable } from 'rxjs';
import { ToolCall } from '../../../services/chat.service';

export interface ToolRenderer {
  toolName: string;
  canRender(toolCall: ToolCall): boolean;
  handleStream?(chunk: string): void;
  onStreamComplete?(): void;
}