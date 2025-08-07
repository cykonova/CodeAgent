import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';

export interface HeaderAction {
  id: string;
  icon: string;
  tooltip: string;
  badge?: number;
  active?: boolean;
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
    MatDividerModule
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
  
  @Output() actionClick = new EventEmitter<string>();
  @Output() clearSession = new EventEmitter<void>();
  
  onActionClick(actionId: string): void {
    this.actionClick.emit(actionId);
  }
  
  onClearSession(): void {
    this.clearSession.emit();
  }
}