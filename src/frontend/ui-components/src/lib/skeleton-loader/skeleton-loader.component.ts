import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'lib-skeleton-loader',
  standalone: true,
  imports: [CommonModule, MatProgressBarModule],
  template: `
    <div class="skeleton-loader" [ngClass]="type">
      @if (type === 'text') {
        <div class="skeleton-text">
          @for (line of lines; track $index) {
            <div class="skeleton-line" [style.width.%]="getLineWidth($index)"></div>
          }
        </div>
      } @else if (type === 'card') {
        <div class="skeleton-card">
          <div class="skeleton-header"></div>
          <div class="skeleton-content">
            @for (line of [1, 2, 3]; track $index) {
              <div class="skeleton-line"></div>
            }
          </div>
        </div>
      } @else if (type === 'table') {
        <div class="skeleton-table">
          @for (row of rows; track $index) {
            <div class="skeleton-row">
              @for (col of columns; track $index) {
                <div class="skeleton-cell"></div>
              }
            </div>
          }
        </div>
      } @else if (type === 'avatar') {
        <div class="skeleton-avatar"></div>
      } @else if (type === 'image') {
        <div class="skeleton-image"></div>
      }
    </div>
  `,
  styles: [`
    .skeleton-loader {
      animation: skeleton-loading 1.5s infinite ease-in-out;
    }

    @keyframes skeleton-loading {
      0%, 100% { opacity: 0.3; }
      50% { opacity: 0.7; }
    }

    .skeleton-line {
      height: 16px;
      background: linear-gradient(90deg, #e0e0e0 25%, #f0f0f0 50%, #e0e0e0 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      margin-bottom: 8px;
      border-radius: 4px;
    }

    .skeleton-card {
      padding: 16px;
      background: var(--mat-app-surface);
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .skeleton-header {
      height: 24px;
      background: linear-gradient(90deg, #e0e0e0 25%, #f0f0f0 50%, #e0e0e0 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      margin-bottom: 16px;
      border-radius: 4px;
      width: 60%;
    }

    .skeleton-table {
      width: 100%;
    }

    .skeleton-row {
      display: flex;
      gap: 16px;
      margin-bottom: 8px;
    }

    .skeleton-cell {
      flex: 1;
      height: 40px;
      background: linear-gradient(90deg, #e0e0e0 25%, #f0f0f0 50%, #e0e0e0 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      border-radius: 4px;
    }

    .skeleton-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: linear-gradient(90deg, #e0e0e0 25%, #f0f0f0 50%, #e0e0e0 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
    }

    .skeleton-image {
      width: 100%;
      height: 200px;
      background: linear-gradient(90deg, #e0e0e0 25%, #f0f0f0 50%, #e0e0e0 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      border-radius: 8px;
    }

    @keyframes shimmer {
      0% { background-position: -200% 0; }
      100% { background-position: 200% 0; }
    }

    :host-context(.dark-theme) .skeleton-line,
    :host-context(.dark-theme) .skeleton-header,
    :host-context(.dark-theme) .skeleton-cell,
    :host-context(.dark-theme) .skeleton-avatar,
    :host-context(.dark-theme) .skeleton-image {
      background: linear-gradient(90deg, #424242 25%, #525252 50%, #424242 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
    }
  `]
})
export class SkeletonLoaderComponent {
  @Input() type: 'text' | 'card' | 'table' | 'avatar' | 'image' = 'text';
  @Input() lines = [1, 2, 3];
  @Input() rows = [1, 2, 3, 4, 5];
  @Input() columns = [1, 2, 3, 4];

  getLineWidth(index: number): number {
    const widths = [100, 80, 90];
    return widths[index % widths.length];
  }
}