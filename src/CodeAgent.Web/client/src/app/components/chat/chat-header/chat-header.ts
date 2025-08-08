import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectChange } from '@angular/material/select';

export interface HeaderAction {
  id: string;
  icon: string;
  tooltip: string;
  badge?: number;
  active?: boolean;
}

export interface Provider {
  id: string;
  name: string;
  type: string;
  enabled: boolean;
}

@Component({
  selector: 'app-chat-header',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    MatChipsModule,
    MatTooltipModule,
    MatDividerModule,
    MatSelectModule,
    MatFormFieldModule
  ],
  templateUrl: './chat-header.html',
  styleUrl: './chat-header.scss'
})
export class ChatHeaderComponent {
  @Input() title: string = 'AI Assistant';
  @Input() subtitle?: string;
  @Input() actions: HeaderAction[] = [];
  @Input() showPermissionsBadge: boolean = false;
  @Input() permissionsCount: number = 0;
  @Input() availableProviders: Provider[] = [];
  @Input() selectedProvider?: string;
  @Input() availableModels: string[] = [];
  @Input() selectedModel?: string;
  
  @Output() actionClick = new EventEmitter<string>();
  @Output() clearSession = new EventEmitter<void>();
  @Output() providerChange = new EventEmitter<string>();
  @Output() modelChange = new EventEmitter<string>();
  
  onActionClick(actionId: string): void {
    this.actionClick.emit(actionId);
  }
  
  onClearSession(): void {
    this.clearSession.emit();
  }
  
  onProviderChange(event: MatSelectChange): void {
    this.providerChange.emit(event.value);
  }
  
  onModelChange(event: MatSelectChange): void {
    this.modelChange.emit(event.value);
  }
  
  getProviderIcon(type: string): string {
    switch (type) {
      case 'openai': return 'auto_awesome';
      case 'claude': return 'psychology';
      case 'ollama': return 'computer';
      case 'lmstudio': return 'desktop_windows';
      default: return 'smart_toy';
    }
  }
}