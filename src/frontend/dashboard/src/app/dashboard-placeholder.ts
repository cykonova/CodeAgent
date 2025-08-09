import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard-placeholder',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard-container">
      <h1>Dashboard</h1>
      <p>Welcome to the Code Agent Dashboard</p>
      <div class="stats">
        <div class="stat-card">
          <h3>Projects</h3>
          <p>0 Active</p>
        </div>
        <div class="stat-card">
          <h3>Agents</h3>
          <p>0 Running</p>
        </div>
        <div class="stat-card">
          <h3>Tasks</h3>
          <p>0 Completed</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 20px;
    }
    h1 {
      color: var(--mat-app-primary);
      margin-bottom: 20px;
    }
    .stats {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 20px;
      margin-top: 30px;
    }
    .stat-card {
      background: var(--mat-app-surface);
      border: 1px solid var(--mat-app-outline);
      border-radius: 8px;
      padding: 20px;
      text-align: center;
    }
    .stat-card h3 {
      margin: 0 0 10px 0;
      color: var(--mat-app-on-surface);
    }
    .stat-card p {
      margin: 0;
      font-size: 24px;
      font-weight: bold;
      color: var(--mat-app-primary);
    }
  `]
})
export class DashboardPlaceholderComponent {}