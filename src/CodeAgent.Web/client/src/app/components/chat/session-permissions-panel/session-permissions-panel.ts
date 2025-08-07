import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatListModule } from '@angular/material/list';
import { SessionPermission } from '../session-permissions-dialog/session-permissions-dialog';

@Component({
  selector: 'app-session-permissions-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatDividerModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatExpansionModule,
    MatTooltipModule,
    MatListModule
  ],
  templateUrl: './session-permissions-panel.html',
  styleUrl: './session-permissions-panel.scss'
})
export class SessionPermissionsPanel {
  @Input() set permissions(value: SessionPermission[]) {
    this._permissions.set(value);
    this.groupPermissions();
  }
  
  @Input() set paths(value: { permitted: string[], blocked: string[] }) {
    this.sessionPaths.set(value);
  }
  
  @Output() permissionsChanged = new EventEmitter<SessionPermission[]>();
  @Output() pathsChanged = new EventEmitter<{ permitted: string[], blocked: string[] }>();
  
  private _permissions = signal<SessionPermission[]>([]);
  sessionPaths = signal<{ permitted: string[], blocked: string[] }>({
    permitted: [],
    blocked: []
  });
  
  newPermittedPath = '';
  newBlockedPath = '';
  
  // Group permissions by category
  filePermissions = signal<SessionPermission[]>([]);
  systemPermissions = signal<SessionPermission[]>([]);
  gitPermissions = signal<SessionPermission[]>([]);
  toolPermissions = signal<SessionPermission[]>([]);
  
  constructor() {
    // Initialize with default permissions if none provided
    if (this._permissions().length === 0) {
      this.initializeDefaultPermissions();
    }
  }
  
  private initializeDefaultPermissions(): void {
    const defaultPermissions: SessionPermission[] = [
      {
        id: 'file-read',
        name: 'Read Files',
        description: 'Allow reading files in permitted directories',
        granted: false,
        icon: 'visibility',
        category: 'file',
        riskLevel: 'low'
      },
      {
        id: 'file-write',
        name: 'Write Files',
        description: 'Allow creating and modifying files',
        granted: false,
        icon: 'edit',
        category: 'file',
        riskLevel: 'medium'
      },
      {
        id: 'file-delete',
        name: 'Delete Files',
        description: 'Allow deleting files and directories',
        granted: false,
        icon: 'delete',
        category: 'file',
        riskLevel: 'high'
      },
      {
        id: 'git-read',
        name: 'Git Status',
        description: 'View git status and history',
        granted: false,
        icon: 'info',
        category: 'git',
        riskLevel: 'low'
      },
      {
        id: 'git-commit',
        name: 'Git Commit',
        description: 'Create git commits',
        granted: false,
        icon: 'check_circle',
        category: 'git',
        riskLevel: 'medium'
      },
      {
        id: 'system-info',
        name: 'System Info',
        description: 'Read system information',
        granted: false,
        icon: 'computer',
        category: 'system',
        riskLevel: 'low'
      },
      {
        id: 'system-execute',
        name: 'Execute Commands',
        description: 'Run system commands',
        granted: false,
        icon: 'terminal',
        category: 'system',
        riskLevel: 'high'
      },
      {
        id: 'tool-search',
        name: 'Search Tools',
        description: 'Use search and grep tools',
        granted: false,
        icon: 'search',
        category: 'tool',
        riskLevel: 'low'
      }
    ];
    
    this._permissions.set(defaultPermissions);
    this.groupPermissions();
  }
  
  private groupPermissions(): void {
    const perms = this._permissions();
    this.filePermissions.set(perms.filter(p => p.category === 'file'));
    this.systemPermissions.set(perms.filter(p => p.category === 'system'));
    this.gitPermissions.set(perms.filter(p => p.category === 'git'));
    this.toolPermissions.set(perms.filter(p => p.category === 'tool'));
  }
  
  togglePermission(permission: SessionPermission): void {
    const updated = this._permissions().map(p =>
      p.id === permission.id ? { ...p, granted: !p.granted } : p
    );
    this._permissions.set(updated);
    this.groupPermissions();
    this.permissionsChanged.emit(updated);
  }
  
  toggleAll(granted: boolean): void {
    const updated = this._permissions().map(p => ({ ...p, granted }));
    this._permissions.set(updated);
    this.groupPermissions();
    this.permissionsChanged.emit(updated);
  }
  
  toggleCategory(category: string, granted: boolean): void {
    const updated = this._permissions().map(p =>
      p.category === category ? { ...p, granted } : p
    );
    this._permissions.set(updated);
    this.groupPermissions();
    this.permissionsChanged.emit(updated);
  }
  
  addPermittedPath(): void {
    if (!this.newPermittedPath.trim()) return;
    
    const updated = {
      ...this.sessionPaths(),
      permitted: [...this.sessionPaths().permitted, this.newPermittedPath.trim()]
    };
    this.sessionPaths.set(updated);
    this.pathsChanged.emit(updated);
    this.newPermittedPath = '';
  }
  
  removePermittedPath(path: string): void {
    const updated = {
      ...this.sessionPaths(),
      permitted: this.sessionPaths().permitted.filter(p => p !== path)
    };
    this.sessionPaths.set(updated);
    this.pathsChanged.emit(updated);
  }
  
  addBlockedPath(): void {
    if (!this.newBlockedPath.trim()) return;
    
    const updated = {
      ...this.sessionPaths(),
      blocked: [...this.sessionPaths().blocked, this.newBlockedPath.trim()]
    };
    this.sessionPaths.set(updated);
    this.pathsChanged.emit(updated);
    this.newBlockedPath = '';
  }
  
  removeBlockedPath(path: string): void {
    const updated = {
      ...this.sessionPaths(),
      blocked: this.sessionPaths().blocked.filter(p => p !== path)
    };
    this.sessionPaths.set(updated);
    this.pathsChanged.emit(updated);
  }
  
  getRiskColor(level: string): string {
    switch (level) {
      case 'low': return 'primary';
      case 'medium': return 'accent';
      case 'high': return 'warn';
      default: return '';
    }
  }
  
  getRiskIcon(level: string): string {
    switch (level) {
      case 'low': return 'check_circle';
      case 'medium': return 'warning';
      case 'high': return 'error';
      default: return 'help';
    }
  }
  
  getGrantedCount(permissions: SessionPermission[]): number {
    return permissions.filter(p => p.granted).length;
  }
  
  getAllPermissions(): SessionPermission[] {
    return this._permissions();
  }
}