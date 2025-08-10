import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

export type StatusType = 'success' | 'warning' | 'error' | 'info' | 'idle' | 'running' | 'paused';

@Component({
  selector: 'app-status-indicator',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatTooltipModule],
  templateUrl: './status-indicator.component.html',
  styleUrls: ['./status-indicator.component.scss']
})
export class StatusIndicatorComponent {
  @Input() status: StatusType = 'idle';
  @Input() style: 'dot' | 'icon' | 'badge' = 'dot';
  @Input() showLabel = false;
  @Input() label?: string;
  @Input() tooltip = '';
  @Input() pulse = false;
  
  getIcon(): string {
    const icons: Record<StatusType, string> = {
      success: 'check_circle',
      warning: 'warning',
      error: 'error',
      info: 'info',
      idle: 'pause_circle',
      running: 'play_circle',
      paused: 'pause_circle'
    };
    return icons[this.status];
  }
  
  getLabel(): string {
    return this.status.charAt(0).toUpperCase() + this.status.slice(1);
  }
}