import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <div class="projects-container">
      <div class="page-header">
        <h1 class="page-title">Projects</h1>
        <button mat-raised-button color="primary">
          <mat-icon>add</mat-icon>
          New Project
        </button>
      </div>
      
      <mat-card>
        <mat-card-content>
          <p>No projects found. Create your first project to get started.</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .projects-container {
      max-width: var(--container-lg);
      margin: 0 auto;
    }
    
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: var(--spacing-lg);
    }
    
    .page-title {
      font-size: 32px;
      font-weight: var(--font-weight-light);
      color: var(--mat-on-surface);
      margin: 0;
    }
  `]
})
export class ProjectsComponent {}