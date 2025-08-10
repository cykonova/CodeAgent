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
    CardComponent
  ],
  templateUrl: './dashboard-home.component.html',
  styleUrl: './dashboard-home.component.scss'
})
export class DashboardHomeComponent {
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

  onCardClick(): void {
    console.log('Card clicked!');
  }
}