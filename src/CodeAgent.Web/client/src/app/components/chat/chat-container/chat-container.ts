import { Component, signal, inject, ViewChild, ElementRef, OnInit, OnDestroy, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { trigger, state, style, transition, animate } from '@angular/animations';
import { Subscription } from 'rxjs';
import { ChatHeaderComponent, HeaderAction, Provider } from '../chat-header/chat-header';
import { MessagesListComponent } from '../messages-list/messages-list';
import { ChatInputComponent } from '../chat-input/chat-input';
import { PanelComponent } from '../../shared/panels/panel/panel';
import { SessionPermissionsPanel } from '../session-permissions-panel/session-permissions-panel';
import { ContextFilesPanel } from '../context-files-panel/context-files-panel';
import { ChatHistoryPanel } from '../chat-history-panel/chat-history-panel';
import { ToolsPanel } from '../tools-panel/tools-panel';
import { ChatService, Message } from '../../../services/chat.service';
import { ToolParserService } from '../../../services/tool-parser.service';
import { SessionPermissionsDialog, SessionPermission } from '../session-permissions-dialog/session-permissions-dialog';

interface ChatRequest {
  message: string;
  provider: string;
  model: string;
  stream?: boolean;
}

@Component({
  selector: 'app-chat-container',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatChipsModule,
    MatBadgeModule,
    MatTooltipModule,
    ChatHeaderComponent,
    MessagesListComponent,
    ChatInputComponent,
    PanelComponent,
    SessionPermissionsPanel,
    ContextFilesPanel,
    ChatHistoryPanel,
    ToolsPanel
  ],
  templateUrl: './chat-container.html',
  styleUrl: './chat-container.scss',
  animations: [
    trigger('slideIn', [
      state('false', style({
        transform: 'translateX(100%)',
        opacity: 0
      })),
      state('true', style({
        transform: 'translateX(0)',
        opacity: 1
      })),
      transition('false <=> true', animate('300ms ease-in-out'))
    ])
  ]
})
export class ChatContainer implements OnInit, OnDestroy {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  
  private chatService = inject(ChatService);
  private toolParser = inject(ToolParserService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private streamSubscription?: Subscription;
  
  messages = signal<Message[]>([]);
  inputMessage = '';
  isLoading = signal(false);
  isStreaming = signal(false);
  currentStreamMessage = signal('');
  
  // Header actions configuration
  headerActions: HeaderAction[] = [
    { id: 'permissions', icon: 'security', tooltip: 'Session Permissions' },
    { id: 'context', icon: 'folder_open', tooltip: 'Context & Files' },
    { id: 'history', icon: 'history', tooltip: 'Chat History' },
    { id: 'tools', icon: 'build', tooltip: 'Available Tools' }
  ];
  
  // Session permissions
  sessionPermissions = signal<SessionPermission[]>([]);
  sessionPaths = signal<{ permitted: string[], blocked: string[] }>({
    permitted: [],
    blocked: []
  });
  activePermissionsCount = signal(0);
  
  // Sidecar state
  showSidecar = signal(false);
  sidecarView = signal<'permissions' | 'context' | 'history' | 'tools'>('permissions');
  
  // Configuration
  useStreaming = true;
  selectedProvider: string = '';
  selectedModel: string = '';
  availableProviders = signal<Provider[]>([]);
  availableModels = signal<string[]>([]);
  
  ngOnInit(): void {
    // Load messages from service
    this.messages = this.chatService.getMessages();
    
    // Load configuration
    this.loadConfiguration();
    
    // Monitor connection state with effect
    effect(() => {
      const state = this.chatService.getConnectionState()();
      if (state === 'connected') {
        console.log('Connected to SignalR hub');
      } else if (state === 'disconnected') {
        console.log('Disconnected from SignalR hub');
      }
    });
  }
  
  ngOnDestroy(): void {
    if (this.streamSubscription) {
      this.streamSubscription.unsubscribe();
    }
  }
  
  private loadConfiguration(): void {
    // Load configuration first to get the default provider
    this.chatService.getConfiguration().subscribe({
      next: (config) => {
        if (config.defaultProvider) {
          this.selectedProvider = config.defaultProvider;
        }
        if (config.defaultModel) {
          this.selectedModel = config.defaultModel;
        }
        
        // After loading config, load providers to populate dropdowns
        this.chatService.getProviders().subscribe({
          next: (providers) => {
            this.availableProviders.set(providers);
            
            // If we have a selected provider, load its models
            if (this.selectedProvider) {
              this.loadModelsForProvider(this.selectedProvider);
            } else if (providers.length > 0) {
              // No provider selected from config, pick first enabled one
              const enabledProvider = providers.find(p => p.enabled);
              if (enabledProvider) {
                this.selectedProvider = enabledProvider.id;
                this.loadModelsForProvider(enabledProvider.id);
              } else {
                // No enabled providers, just pick the first one
                this.selectedProvider = providers[0].id;
                this.loadModelsForProvider(providers[0].id);
              }
            }
          },
          error: (error) => {
            console.error('Failed to load providers:', error);
          }
        });
      },
      error: (error) => {
        console.error('Failed to load configuration:', error);
        
        // Even if config fails, try to load providers
        this.chatService.getProviders().subscribe({
          next: (providers) => {
            this.availableProviders.set(providers);
            if (providers.length > 0) {
              const enabledProvider = providers.find(p => p.enabled) || providers[0];
              this.selectedProvider = enabledProvider.id;
              this.loadModelsForProvider(enabledProvider.id);
            }
          },
          error: (err) => {
            console.error('Failed to load providers:', err);
          }
        });
      }
    });
  }
  
  private loadModelsForProvider(providerId: string): void {
    // Set default models based on provider type
    const provider = this.availableProviders().find(p => p.id === providerId);
    if (!provider) return;
    
    const defaultModels: Record<string, string[]> = {
      openai: ['gpt-4', 'gpt-4-turbo', 'gpt-3.5-turbo'],
      claude: ['claude-3-5-sonnet-20241022', 'claude-3-haiku-20240307', 'claude-3-opus-20240229'],
      ollama: ['llama3.2', 'qwen2.5', 'gemma2'],
      lmstudio: ['local-model']
    };
    
    const models = defaultModels[provider.type] || ['default-model'];
    this.availableModels.set(models);
    
    // Set default model if none selected
    if (!this.selectedModel && models.length > 0) {
      this.selectedModel = models[0];
    }
  }
  
  onHeaderAction(actionId: string): void {
    this.toggleSidecar(actionId as 'permissions' | 'context' | 'history' | 'tools');
  }
  
  sendMessage(message?: string): void {
    const messageContent = message || this.inputMessage.trim();
    if (!messageContent || this.isLoading() || this.isStreaming()) return;
    
    // Add user message
    const userMessage: Message = {
      id: crypto.randomUUID(),
      content: messageContent,
      role: 'user',
      timestamp: new Date()
    };
    
    this.chatService.addMessage(userMessage);
    this.inputMessage = '';
    this.scrollToBottom();
    
    // Prepare request
    const request: ChatRequest = {
      message: messageContent,
      provider: this.selectedProvider,
      model: this.selectedModel,
      stream: this.useStreaming
    };
    
    if (this.useStreaming) {
      this.sendStreamingMessage(request);
    } else {
      this.sendRegularMessage(request);
    }
  }
  
  private sendRegularMessage(request: ChatRequest): void {
    this.isLoading.set(true);
    
    this.chatService.sendMessage(request).subscribe({
      next: (response) => {
        // Extract tool calls from the response
        const toolCalls = this.toolParser.extractToolCalls(response);
        
        const assistantMessage: Message = {
          id: response.id || crypto.randomUUID(),
          content: response.content || '', // Preserve original content for history reload
          role: 'assistant',
          timestamp: new Date(),
          toolCalls: toolCalls.length > 0 ? toolCalls : response.toolCalls,
          metadata: {
            usage: response.usage,
            provider: this.selectedProvider,
            model: this.selectedModel
          }
        };
        
        this.chatService.addMessage(assistantMessage);
        this.isLoading.set(false);
        this.scrollToBottom();
      },
      error: (error) => {
        this.isLoading.set(false);
        this.handleError(error);
      }
    });
  }
  
  private sendStreamingMessage(request: ChatRequest): void {
    this.isStreaming.set(true);
    this.currentStreamMessage.set('');
    
    // Create placeholder message for streaming
    const streamMessageId = crypto.randomUUID();
    const streamMessage: Message = {
      id: streamMessageId,
      content: '',
      role: 'assistant',
      timestamp: new Date(),
      toolCalls: []
    };
    
    this.chatService.addMessage(streamMessage);
    
    let accumulatedContent = '';
    
    this.streamSubscription = this.chatService.streamMessage(request).subscribe({
      next: (chunk) => {
        accumulatedContent += chunk;
        
        // Parse the accumulated content for tool calls
        const parsed = this.toolParser.handleStreamChunk(streamMessageId, accumulatedContent);
        
        // Update the message with parsed tool calls
        this.messages.update(msgs => 
          msgs.map(msg => {
            if (msg.id === streamMessageId) {
              // If we have tool calls, use them; otherwise show raw content
              if (parsed.toolCalls.length > 0) {
                return { 
                  ...msg, 
                  toolCalls: parsed.toolCalls,
                  content: '' // Clear content when we have tool calls
                };
              } else {
                // Still parsing, show accumulated content
                return { 
                  ...msg, 
                  content: accumulatedContent,
                  toolCalls: []
                };
              }
            }
            return msg;
          })
        );
        
        this.scrollToBottom();
      },
      error: (error) => {
        this.isStreaming.set(false);
        this.toolParser.clearStreamBuffer(streamMessageId);
        this.handleError(error);
      },
      complete: () => {
        this.isStreaming.set(false);
        this.currentStreamMessage.set('');
        
        // Final parse to ensure all tool calls are extracted
        const finalParsed = this.toolParser.parseMessage(accumulatedContent);
        if (finalParsed.toolCalls.length > 0) {
          this.messages.update(msgs => 
            msgs.map(msg => 
              msg.id === streamMessageId 
                ? { ...msg, toolCalls: finalParsed.toolCalls, content: '' }
                : msg
            )
          );
        }
        
        this.toolParser.clearStreamBuffer(streamMessageId);
      }
    });
  }
  
  private handleError(error: any): void {
    console.error('Chat error:', error);
    
    let errorMessage = 'Failed to send message';
    if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    this.snackBar.open(errorMessage, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
    
    // Add error message to chat
    const errorMsg: Message = {
      id: crypto.randomUUID(),
      content: `Error: ${errorMessage}`,
      role: 'system',
      timestamp: new Date()
    };
    
    this.chatService.addMessage(errorMsg);
    
    // Try to reconnect if it's a connection issue
    if (errorMessage.includes('connection') || errorMessage.includes('network')) {
      setTimeout(() => {
        this.chatService.reconnect();
      }, 2000);
    }
  }
  
  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.messagesContainer) {
        const element = this.messagesContainer.nativeElement;
        element.scrollTop = element.scrollHeight;
      }
    }, 100);
  }
  
  // Handle keyboard shortcuts
  handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      if (event.shiftKey) {
        // Shift+Enter: allow new line (default behavior)
        return;
      } else {
        // Enter: send message
        event.preventDefault();
        this.sendMessage();
      }
    }
  }
  
  // Session permissions management
  openSessionPermissions(): void {
    const dialogRef = this.dialog.open(SessionPermissionsDialog, {
      width: '700px',
      data: {
        currentSessionPermissions: this.sessionPermissions(),
        permittedPaths: this.sessionPaths().permitted,
        blockedPaths: this.sessionPaths().blocked
      }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.sessionPermissions.set(result.permissions);
        this.sessionPaths.set(result.paths);
        this.updateActivePermissionsCount();
        
        // Send permissions update to backend
        this.updateBackendPermissions();
        
        this.snackBar.open('Session permissions updated', 'Close', {
          duration: 3000
        });
      }
    });
  }
  
  private updateActivePermissionsCount(): void {
    const count = this.sessionPermissions().filter(p => p.granted).length;
    this.activePermissionsCount.set(count);
  }
  
  private updateBackendPermissions(): void {
    // TODO: Send session permissions to backend
    const permissions = {
      permissions: this.sessionPermissions().filter(p => p.granted),
      paths: this.sessionPaths()
    };
    
    // This would call an API endpoint to update session permissions
    console.log('Updating backend with session permissions:', permissions);
  }
  
  clearSession(): void {
    if (confirm('This will clear all messages and session permissions. Continue?')) {
      // Clear messages from backend and local state
      this.chatService.clearHistory().subscribe({
        next: () => {
          // Clear local state after successful backend clear
          this.messages.set([]);
          
          // Clear session permissions
          this.sessionPermissions.set([]);
          this.sessionPaths.set({ permitted: [], blocked: [] });
          this.activePermissionsCount.set(0);
          
          // Notify backend about cleared permissions
          this.updateBackendPermissions();
          
          this.snackBar.open('Session cleared successfully', 'Close', {
            duration: 2000,
            panelClass: ['success-snackbar']
          });
        },
        error: (error) => {
          console.error('Failed to clear session:', error);
          this.snackBar.open('Failed to clear session', 'Close', {
            duration: 3000,
            panelClass: ['error-snackbar']
          });
        }
      });
    }
  }
  
  hasActivePermissions(): boolean {
    return this.activePermissionsCount() > 0;
  }
  
  // Sidecar management
  toggleSidecar(view: 'permissions' | 'context' | 'history' | 'tools'): void {
    if (this.showSidecar() && this.sidecarView() === view) {
      // Close if clicking same view
      this.showSidecar.set(false);
    } else {
      // Open with new view
      this.sidecarView.set(view);
      this.showSidecar.set(true);
    }
  }
  
  closeSidecar(): void {
    this.showSidecar.set(false);
  }
  
  getSidecarTitle(): string {
    switch (this.sidecarView()) {
      case 'permissions': return 'Session Permissions';
      case 'context': return 'Context & Files';
      case 'history': return 'Chat History';
      case 'tools': return 'Available Tools';
      default: return '';
    }
  }
  
  // Handle permissions changes from panel
  onPermissionsChanged(permissions: SessionPermission[]): void {
    this.sessionPermissions.set(permissions);
    this.updateActivePermissionsCount();
    this.updateBackendPermissions();
  }
  
  onPathsChanged(paths: { permitted: string[], blocked: string[] }): void {
    this.sessionPaths.set(paths);
    this.updateBackendPermissions();
  }
  
  // Provider and model selection handlers
  onProviderChange(providerId: string): void {
    this.selectedProvider = providerId;
    this.loadModelsForProvider(providerId);
    
    // Update configuration
    this.chatService.updateConfiguration({
      defaultProvider: providerId,
      defaultModel: this.selectedModel
    }).subscribe({
      error: (error) => {
        console.error('Failed to update provider configuration:', error);
      }
    });
  }
  
  onModelChange(modelId: string): void {
    this.selectedModel = modelId;
    
    // Update configuration
    this.chatService.updateConfiguration({
      defaultProvider: this.selectedProvider,
      defaultModel: modelId
    }).subscribe({
      error: (error) => {
        console.error('Failed to update model configuration:', error);
      }
    });
  }
}