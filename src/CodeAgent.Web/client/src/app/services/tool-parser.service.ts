import { Injectable, signal } from '@angular/core';
import { ToolCall } from '../components/chat/tool-renderers/tool-renderer.interface';

export interface ParsedToolMessage {
  toolCalls: ToolCall[];
  isComplete: boolean;
  rawContent: string;
}

@Injectable({
  providedIn: 'root'
})
export class ToolParserService {
  // Buffer for streaming content
  private streamBuffers = new Map<string, string>();
  
  /**
   * Parse a complete message that may contain tool calls
   */
  parseMessage(content: string): ParsedToolMessage {
    const toolCalls: ToolCall[] = [];
    
    try {
      // Try to parse as JSON first (complete tool call)
      const parsed = JSON.parse(content);
      
      if (Array.isArray(parsed)) {
        // Multiple tool calls
        toolCalls.push(...parsed.map(this.normalizeToolCall));
      } else if (parsed.name || parsed.tool_name) {
        // Single tool call
        toolCalls.push(this.normalizeToolCall(parsed));
      }
      
      return {
        toolCalls,
        isComplete: true,
        rawContent: content
      };
    } catch (e) {
      // Not valid JSON, might be partial or plain text
      // Check for common tool call patterns
      const toolCallPattern = /\{"(?:name|tool_name)":\s*"([^"]+)"/;
      const match = content.match(toolCallPattern);
      
      if (match) {
        // Partial tool call detected
        return {
          toolCalls: [],
          isComplete: false,
          rawContent: content
        };
      }
      
      // Treat as respond_to_user if it's plain text
      if (content.trim().length > 0) {
        toolCalls.push({
          name: 'respond_to_user',
          arguments: {
            message: content
          }
        });
      }
      
      return {
        toolCalls,
        isComplete: true,
        rawContent: content
      };
    }
  }
  
  /**
   * Handle streaming content and try to parse tool calls
   */
  handleStreamChunk(messageId: string, chunk: string): ParsedToolMessage {
    // Get or create buffer for this message
    let buffer = this.streamBuffers.get(messageId) || '';
    buffer += chunk;
    this.streamBuffers.set(messageId, buffer);
    
    // Try to parse complete tool calls from buffer
    const toolCalls: ToolCall[] = [];
    let remainingBuffer = buffer;
    
    // Look for complete JSON objects in the buffer
    const jsonExtractor = /(\{[^{}]*\{[^{}]*\}[^{}]*\}|\{[^{}]*\})/g;
    let match;
    let lastIndex = 0;
    
    while ((match = jsonExtractor.exec(buffer)) !== null) {
      try {
        const jsonStr = match[0];
        const parsed = JSON.parse(jsonStr);
        
        if (parsed.name || parsed.tool_name) {
          toolCalls.push(this.normalizeToolCall(parsed));
          lastIndex = match.index + match[0].length;
        }
      } catch (e) {
        // Not a complete JSON object yet
      }
    }
    
    // Update buffer to remove parsed content
    if (lastIndex > 0) {
      remainingBuffer = buffer.substring(lastIndex);
      this.streamBuffers.set(messageId, remainingBuffer);
    }
    
    // Check if we have a complete message
    const isComplete = this.isMessageComplete(remainingBuffer);
    
    return {
      toolCalls,
      isComplete,
      rawContent: buffer
    };
  }
  
  /**
   * Clear stream buffer for a message
   */
  clearStreamBuffer(messageId: string): void {
    this.streamBuffers.delete(messageId);
  }
  
  /**
   * Normalize tool call structure
   */
  private normalizeToolCall(raw: any): ToolCall {
    return {
      id: raw.id || crypto.randomUUID(),
      name: raw.name || raw.tool_name || 'unknown',
      arguments: raw.arguments || raw.parameters || raw.args || {},
      parameters: raw.parameters || raw.arguments || raw.args || {},
      result: raw.result,
      isStreaming: raw.isStreaming || false
    };
  }
  
  /**
   * Check if a buffer contains a complete message
   */
  private isMessageComplete(buffer: string): boolean {
    // Check for common end-of-message indicators
    if (buffer.trim().endsWith('}')) {
      // Try to parse to verify it's complete JSON
      try {
        JSON.parse(buffer);
        return true;
      } catch {
        return false;
      }
    }
    
    // Check for SSE end marker
    if (buffer.includes('[DONE]') || buffer.includes('data: [DONE]')) {
      return true;
    }
    
    return false;
  }
  
  /**
   * Extract tool calls from assistant response
   * Handles both single and multiple tool calls
   */
  extractToolCalls(response: any): ToolCall[] {
    const toolCalls: ToolCall[] = [];
    
    // Check for tool_calls array
    if (response.tool_calls && Array.isArray(response.tool_calls)) {
      toolCalls.push(...response.tool_calls.map(this.normalizeToolCall));
    }
    // Check for toolCalls array
    else if (response.toolCalls && Array.isArray(response.toolCalls)) {
      toolCalls.push(...response.toolCalls.map(this.normalizeToolCall));
    }
    // Check for single tool call
    else if (response.tool_call) {
      toolCalls.push(this.normalizeToolCall(response.tool_call));
    }
    // Check if response itself is a tool call
    else if (response.name || response.tool_name) {
      toolCalls.push(this.normalizeToolCall(response));
    }
    // If there's content but no tool calls, treat as respond_to_user
    else if (response.content && typeof response.content === 'string') {
      // Check if content is JSON
      try {
        const parsed = JSON.parse(response.content);
        if (parsed.name || parsed.tool_name) {
          toolCalls.push(this.normalizeToolCall(parsed));
        } else if (Array.isArray(parsed)) {
          toolCalls.push(...parsed.map(this.normalizeToolCall));
        }
      } catch {
        // Content is plain text, wrap in respond_to_user
        toolCalls.push({
          name: 'respond_to_user',
          arguments: {
            message: response.content
          }
        });
      }
    }
    
    return toolCalls;
  }
}