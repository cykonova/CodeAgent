import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  template: `
    <mat-card class="stat-card" [class.clickable]="clickable">
      <mat-card-content>
        <div class="stat-header">
          <mat-icon [style.color]="iconColor">{{ icon }}</mat-icon>
          <span class="stat-label">{{ label }}</span>
        </div>
        <div class="stat-value">{{ value }}</div>
        <div class="stat-change" *ngIf="change !== undefined" [class.positive]="change > 0" [class.negative]="change < 0">
          <mat-icon>{{ change > 0 ? 'trending_up' : change < 0 ? 'trending_down' : 'trending_flat' }}</mat-icon>
          <span>{{ Math.abs(change) }}%</span>
        </div>
        <div class="stat-description" *ngIf="description">{{ description }}</div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .stat-card {
      height: 100%;
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .stat-card.clickable {
      cursor: pointer;
    }
    .stat-card.clickable:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.1);
    }
    .stat-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 16px;
    }
    .stat-label {
      font-size: 14px;
      opacity: 0.8;
    }
    .stat-value {
      font-size: 32px;
      font-weight: 500;
      margin-bottom: 8px;
    }
    .stat-change {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 14px;
    }
    .stat-change.positive {
      color: #4caf50;
    }
    .stat-change.negative {
      color: #f44336;
    }
    .stat-description {
      font-size: 12px;
      opacity: 0.7;
      margin-top: 8px;
    }
  `]
})
export class StatCardComponent {
  @Input() label!: string;
  @Input() value!: string | number;
  @Input() icon = 'analytics';
  @Input() iconColor = 'primary';
  @Input() change?: number;
  @Input() description?: string;
  @Input() clickable = false;
  
  Math = Math;
}