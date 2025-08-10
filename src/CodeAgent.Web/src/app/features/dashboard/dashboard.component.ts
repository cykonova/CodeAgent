import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatGridListModule } from '@angular/material/grid-list';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatGridListModule
  ],
  template: `
    <div class="dashboard-container">
      <h1 class="page-title">Dashboard</h1>
      
      <div class="stats-grid">
        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-icon">
              <mat-icon color="primary">folder</mat-icon>
            </div>
            <div class="stat-value">12</div>
            <div class="stat-label">Active Projects</div>
          </mat-card-content>
        </mat-card>
        
        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-icon">
              <mat-icon color="accent">smart_toy</mat-icon>
            </div>
            <div class="stat-value">8</div>
            <div class="stat-label">Active Agents</div>
          </mat-card-content>
        </mat-card>
        
        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-icon">
              <mat-icon color="warn">task_alt</mat-icon>
            </div>
            <div class="stat-value">156</div>
            <div class="stat-label">Tasks Completed</div>
          </mat-card-content>
        </mat-card>
        
        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-icon">
              <mat-icon>trending_up</mat-icon>
            </div>
            <div class="stat-value">94%</div>
            <div class="stat-label">Success Rate</div>
          </mat-card-content>
        </mat-card>
      </div>
      
      <mat-card class="mt-lg">
        <mat-card-header>
          <mat-card-title>Recent Activity</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>No recent activity to display.</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .dashboard-container {
      max-width: var(--container-lg);
      margin: 0 auto;
    }
    
    .page-title {
      font-size: 32px;
      font-weight: var(--font-weight-light);
      margin-bottom: var(--spacing-lg);
      color: var(--mat-on-surface);
    }
    
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: var(--spacing-md);
      margin-bottom: var(--spacing-lg);
    }
    
    .stat-card {
      text-align: center;
      
      .stat-icon {
        margin-bottom: var(--spacing-sm);
        
        mat-icon {
          font-size: 48px;
          width: 48px;
          height: 48px;
        }
      }
      
      .stat-value {
        font-size: 36px;
        font-weight: var(--font-weight-bold);
        color: var(--mat-primary);
        margin-bottom: var(--spacing-xs);
      }
      
      .stat-label {
        font-size: 14px;
        color: var(--mat-on-surface-variant);
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }
    }
  `]
})
export class DashboardComponent {}