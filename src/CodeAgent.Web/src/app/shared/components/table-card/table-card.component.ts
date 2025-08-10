import { Component, Input, Output, EventEmitter, ViewChild, OnInit, AfterViewInit, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CardComponent } from '../card/card.component';
import { TableColumn, TableAction, TableConfig } from '../../models/table.model';

@Component({
  selector: 'app-table-card',
  standalone: true,
  imports: [
    CommonModule,
    CardComponent,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ],
  templateUrl: './table-card.component.html',
  styleUrl: './table-card.component.scss'
})
export class TableCardComponent implements OnInit, AfterViewInit, OnChanges {
  @Input() title?: string;
  @Input() subtitle?: string;
  @Input() data: any[] = [];
  @Input() config!: TableConfig;
  @Output() rowClick = new EventEmitter<any>();

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  dataSource!: MatTableDataSource<any>;
  displayedColumns: string[] = [];

  ngOnInit(): void {
    // Initialize data source
    this.dataSource = new MatTableDataSource(this.data);
    
    // Set up displayed columns
    this.displayedColumns = this.config.columns.map(col => col.key);
    
    // Add actions column if actions are defined
    if (this.config.actions && this.config.actions.length > 0) {
      this.displayedColumns.push('actions');
    }
  }

  ngAfterViewInit(): void {
    // Set up pagination if enabled
    if (this.config.showPagination !== false && this.paginator) {
      this.dataSource.paginator = this.paginator;
    }

    // Set up sorting
    if (this.sort) {
      this.dataSource.sort = this.sort;
    }
  }

  ngOnChanges(): void {
    if (this.dataSource) {
      this.dataSource.data = this.data;
    }
  }

  onRowClick(row: any): void {
    if (this.config.hoverable) {
      this.rowClick.emit(row);
    }
  }

  getColumnValue(element: any, column: TableColumn): any {
    const value = element[column.key];
    
    if (column.type === 'date' && value) {
      return new Date(value);
    }
    
    return value;
  }

  getStatusClass(status: string): string {
    const statusLower = status?.toLowerCase() || '';
    
    if (statusLower === 'active' || statusLower === 'success') {
      return 'status-active';
    } else if (statusLower === 'inactive' || statusLower === 'disabled') {
      return 'status-inactive';
    } else if (statusLower === 'pending' || statusLower === 'warning') {
      return 'status-pending';
    } else if (statusLower === 'error' || statusLower === 'failed') {
      return 'status-error';
    }
    
    return 'status-default';
  }

  getPageSizeOptions(): number[] {
    return this.config.pageSizeOptions || [5, 10, 25, 100];
  }

  getPageSize(): number {
    return this.config.pageSize || 10;
  }

  get tableClasses(): Record<string, boolean> {
    return {
      'striped': this.config.striped || false,
      'hoverable': this.config.hoverable || false
    };
  }
}
