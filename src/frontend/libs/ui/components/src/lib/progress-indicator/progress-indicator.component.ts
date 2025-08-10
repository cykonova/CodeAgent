import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'lib-progress-indicator',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatIconModule
  ],
  template: `
    <div class="progress-indicator" [ngClass]="size">
      @switch (type) {
        @case ('spinner') {
          <mat-spinner
            [diameter]="getDiameter()"
            [strokeWidth]="getStrokeWidth()"
            [mode]="mode"
            [value]="value">
          </mat-spinner>
        }
        @case ('bar') {
          <mat-progress-bar
            mode="indeterminate"
            [value]="value"
            [bufferValue]="bufferValue">
          </mat-progress-bar>
        }
        @case ('circular') {
          <div class="circular-progress">
            <svg [attr.width]="getDiameter()" [attr.height]="getDiameter()">
              <circle
                class="progress-background"
                [attr.cx]="getDiameter() / 2"
                [attr.cy]="getDiameter() / 2"
                [attr.r]="getRadius()"
                [attr.stroke-width]="getStrokeWidth()"
                fill="none"
              />
              <circle
                class="progress-value"
                [attr.cx]="getDiameter() / 2"
                [attr.cy]="getDiameter() / 2"
                [attr.r]="getRadius()"
                [attr.stroke-width]="getStrokeWidth()"
                [attr.stroke-dasharray]="getCircumference()"
                [attr.stroke-dashoffset]="getProgress()"
                fill="none"
              />
            </svg>
            @if (showPercentage && mode === 'determinate') {
              <div class="percentage">{{ value }}%</div>
            }
          </div>
        }
        @case ('dots') {
          <div class="loading-dots">
            @for (dot of [1, 2, 3]; track $index) {
              <span class="dot" [style.animation-delay.ms]="$index * 200"></span>
            }
          </div>
        }
      }
      @if (label) {
        <div class="progress-label">{{ label }}</div>
      }
    </div>
  `,
  styles: [`
    .progress-indicator {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }

    .progress-indicator.small mat-spinner {
      transform: scale(0.5);
    }

    .progress-indicator.large mat-spinner {
      transform: scale(1.5);
    }

    .circular-progress {
      position: relative;
      display: inline-block;
    }

    .progress-background {
      stroke: var(--mat-app-outline);
      opacity: 0.2;
    }

    .progress-value {
      stroke: var(--mat-app-primary);
      transform: rotate(-90deg);
      transform-origin: center;
      transition: stroke-dashoffset 0.3s ease;
    }

    .percentage {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      font-size: 14px;
      font-weight: 500;
      color: var(--mat-app-on-surface);
    }

    .loading-dots {
      display: flex;
      gap: 4px;
    }

    .dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background-color: var(--mat-app-primary);
      animation: dot-pulse 1.5s infinite ease-in-out;
    }

    @keyframes dot-pulse {
      0%, 60%, 100% {
        transform: scale(1);
        opacity: 1;
      }
      30% {
        transform: scale(1.5);
        opacity: 0.7;
      }
    }

    .progress-label {
      margin-top: 8px;
      font-size: 14px;
      color: var(--mat-app-on-surface-variant);
      text-align: center;
    }

    mat-progress-bar {
      width: 100%;
    }

    .progress-indicator.small {
      .circular-progress svg {
        width: 24px;
        height: 24px;
      }
      .percentage {
        font-size: 10px;
      }
      .dot {
        width: 6px;
        height: 6px;
      }
    }

    .progress-indicator.large {
      .circular-progress svg {
        width: 64px;
        height: 64px;
      }
      .percentage {
        font-size: 18px;
      }
      .dot {
        width: 12px;
        height: 12px;
      }
    }
  `]
})
export class ProgressIndicatorComponent {
  @Input() type: 'spinner' | 'bar' | 'circular' | 'dots' = 'spinner';
  @Input() mode: 'determinate' | 'indeterminate' = 'indeterminate';
  @Input() value = 0;
  @Input() bufferValue = 0;
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
  @Input() showPercentage = true;
  @Input() label = '';

  getDiameter(): number {
    const sizes = { small: 24, medium: 40, large: 64 };
    return sizes[this.size];
  }

  getStrokeWidth(): number {
    const widths = { small: 3, medium: 4, large: 5 };
    return widths[this.size];
  }

  getRadius(): number {
    return (this.getDiameter() - this.getStrokeWidth()) / 2;
  }

  getCircumference(): number {
    return 2 * Math.PI * this.getRadius();
  }

  getProgress(): number {
    const circumference = this.getCircumference();
    return circumference - (this.value / 100) * circumference;
  }
}