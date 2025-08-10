import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CardComponent } from '../../../shared/components/card/card.component';
import { TableCardComponent } from '../../../shared/components/table-card/table-card.component';
import { LoadingOverlayComponent } from '../../../shared/components/loading-overlay/loading-overlay.component';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader.component';
import { StatusIndicatorComponent } from '../../../shared/components/status-indicator/status-indicator.component';
import { ProgressIndicatorComponent } from '../../../shared/components/progress-indicator/progress-indicator.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TableConfig } from '../../../shared/models/table.model';

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    CardComponent,
    TableCardComponent,
    LoadingOverlayComponent,
    SkeletonLoaderComponent,
    StatusIndicatorComponent,
    ProgressIndicatorComponent,
    EmptyStateComponent
  ],
  templateUrl: './dashboard-home.component.html',
  styleUrl: './dashboard-home.component.scss'
})
export class DashboardHomeComponent {
  // Demo states for new components
  showLoadingOverlay = false;
  showSkeletons = false;
  progressValue = 65;
  currentStep = 2;
  
  // Sample data for testing
  stats = [
    { label: 'Active Projects', value: 12, icon: 'folder', color: 'primary' },
    { label: 'Running Agents', value: 3, icon: 'smart_toy', color: 'accent' },
    { label: 'Completed Tasks', value: 245, icon: 'check_circle', color: 'primary' },
    { label: 'Error Rate', value: '0.2%', icon: 'error', color: 'warn' }
  ];

  tableData = [
    { name: 'Project Alpha', status: 'Active', lastUpdated: '2 hours ago' },
    { name: 'Project Beta', status: 'Completed', lastUpdated: '1 day ago' },
    { name: 'Project Gamma', status: 'In Progress', lastUpdated: '5 minutes ago' }
  ];

  displayedColumns: string[] = ['name', 'status', 'lastUpdated'];

  // Sample data for table card
  projectData = [
    { id: 1, name: 'E-Commerce Scraper', status: 'Active', created: new Date('2024-01-15'), agents: 3, progress: 75 },
    { id: 2, name: 'Code Review Bot', status: 'Pending', created: new Date('2024-02-01'), agents: 2, progress: 30 },
    { id: 3, name: 'Data Pipeline', status: 'Active', created: new Date('2024-01-20'), agents: 5, progress: 90 },
    { id: 4, name: 'Test Automation', status: 'Inactive', created: new Date('2023-12-10'), agents: 0, progress: 100 },
    { id: 5, name: 'Documentation Generator', status: 'Error', created: new Date('2024-02-10'), agents: 1, progress: 45 },
    { id: 6, name: 'API Monitor', status: 'Active', created: new Date('2024-02-05'), agents: 4, progress: 60 },
    { id: 7, name: 'Security Scanner', status: 'Pending', created: new Date('2024-02-12'), agents: 2, progress: 15 },
    { id: 8, name: 'Performance Tester', status: 'Active', created: new Date('2024-01-25'), agents: 3, progress: 80 }
  ];

  tableConfig: TableConfig = {
    columns: [
      { key: 'name', label: 'Project Name', sortable: true },
      { key: 'status', label: 'Status', type: 'status', width: '120px' },
      { key: 'created', label: 'Created', type: 'date', sortable: true },
      { key: 'agents', label: 'Agents', type: 'number', align: 'center', width: '80px' },
      { key: 'progress', label: 'Progress (%)', type: 'number', align: 'center', width: '100px', sortable: true }
    ],
    actions: [
      { 
        icon: 'visibility', 
        label: 'View Details', 
        color: 'primary', 
        callback: (row) => this.viewProject(row) 
      },
      { 
        icon: 'edit', 
        label: 'Edit', 
        color: 'primary', 
        callback: (row) => this.editProject(row) 
      },
      { 
        icon: 'delete', 
        label: 'Delete', 
        color: 'warn', 
        callback: (row) => this.deleteProject(row) 
      }
    ],
    pageSize: 5,
    pageSizeOptions: [5, 10, 25, 50],
    showPagination: true,
    striped: true,
    hoverable: true
  };

  onCardClick(): void {
    console.log('Card clicked!');
  }

  onProjectSelect(project: any): void {
    console.log('Project selected:', project);
  }

  viewProject(project: any): void {
    console.log('View project:', project);
  }

  editProject(project: any): void {
    console.log('Edit project:', project);
  }

  deleteProject(project: any): void {
    console.log('Delete project:', project);
  }

  // Demo methods for new components
  toggleLoadingOverlay(): void {
    this.showLoadingOverlay = true;
    setTimeout(() => {
      this.showLoadingOverlay = false;
    }, 3000);
  }

  toggleSkeletons(): void {
    this.showSkeletons = !this.showSkeletons;
  }

  incrementProgress(): void {
    this.progressValue = Math.min(100, this.progressValue + 10);
  }

  nextStep(): void {
    this.currentStep = Math.min(3, this.currentStep + 1);
  }

  resetDemo(): void {
    this.progressValue = 0;
    this.currentStep = 0;
  }

  handleEmptyAction(): void {
    console.log('Empty state action clicked');
  }
}