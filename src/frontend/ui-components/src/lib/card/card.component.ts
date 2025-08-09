import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatButtonModule],
  template: `
    <mat-card [class]="'app-card ' + class">
      <mat-card-header *ngIf="title || subtitle">
        <mat-card-title>
          <mat-icon *ngIf="icon" class="card-icon">{{ icon }}</mat-icon>
          {{ title }}
        </mat-card-title>
        <mat-card-subtitle *ngIf="subtitle">{{ subtitle }}</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <ng-content></ng-content>
      </mat-card-content>
      <mat-card-actions *ngIf="showActions" align="end">
        <ng-content select="[card-actions]"></ng-content>
      </mat-card-actions>
    </mat-card>
  `,
  styles: [`
    .app-card {
      margin-bottom: 16px;
    }
    .card-icon {
      margin-right: 8px;
      vertical-align: middle;
    }
    mat-card-header {
      margin-bottom: 16px;
    }
  `]
})
export class CardComponent {
  @Input() title?: string;
  @Input() subtitle?: string;
  @Input() icon?: string;
  @Input() class = '';
  @Input() showActions = false;
}