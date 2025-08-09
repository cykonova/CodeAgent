import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'lib-loading-overlay',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    @if (isLoading) {
      <div class="loading-overlay" [class.backdrop]="backdrop">
        <div class="loading-content">
          <mat-spinner [diameter]="48"></mat-spinner>
          @if (message) {
            <p class="loading-message">{{ message }}</p>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .loading-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }

    .loading-overlay.backdrop {
      background-color: rgba(0, 0, 0, 0.3);
      backdrop-filter: blur(2px);
    }

    :host-context(.dark-theme) .loading-overlay.backdrop {
      background-color: rgba(0, 0, 0, 0.5);
    }

    .loading-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 16px;
      padding: 24px;
      background: var(--mat-app-surface);
      border-radius: 8px;
      box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
    }

    .loading-message {
      margin: 0;
      font-size: 14px;
      color: var(--mat-app-on-surface-variant);
      text-align: center;
      max-width: 200px;
    }
  `]
})
export class LoadingOverlayComponent {
  @Input() isLoading = false;
  @Input() message = '';
  @Input() backdrop = true;
}