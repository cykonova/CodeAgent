import { Component, Inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';

export interface SessionPermission {
  id: string;
  name: string;
  description: string;
  granted: boolean;
  icon: string;
  category: 'file' | 'system' | 'git' | 'tool';
  riskLevel: 'low' | 'medium' | 'high';
}

export interface SessionPermissionsData {
  requestedPermissions: SessionPermission[];
  currentSessionPermissions: SessionPermission[];
  permittedPaths: string[];
  blockedPaths: string[];
}

@Component({
  selector: 'app-session-permissions-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatDividerModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatExpansionModule,
    MatTooltipModule
  ],
  templateUrl: './session-permissions-dialog.html',
  styleUrl: './session-permissions-dialog.scss'
})
export class SessionPermissionsDialog {
  permissions = signal<SessionPermission[]>([]);
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
  
  constructor(
    public dialogRef: MatDialogRef<SessionPermissionsDialog>,
    @Inject(MAT_DIALOG_DATA) public data: SessionPermissionsData
  ) {
    // Initialize permissions
    if (data.requestedPermissions) {
      this.permissions.set(data.requestedPermissions);
    } else {
      // Default permissions structure
      this.permissions.set([
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
          id: 'git-push',
          name: 'Git Push',
          description: 'Push to remote repositories',
          granted: false,
          icon: 'cloud_upload',
          category: 'git',
          riskLevel: 'high'
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
        },
        {
          id: 'tool-ai',
          name: 'AI Tools',
          description: 'Use AI-powered code analysis',
          granted: false,
          icon: 'psychology',
          category: 'tool',
          riskLevel: 'medium'
        }
      ]);
    }
    
    // Load current session permissions
    if (data.currentSessionPermissions) {
      this.permissions.update(perms => 
        perms.map(p => {
          const current = data.currentSessionPermissions.find(cp => cp.id === p.id);
          return current ? { ...p, granted: current.granted } : p;
        })
      );
    }
    
    // Load paths
    if (data.permittedPaths || data.blockedPaths) {
      this.sessionPaths.set({
        permitted: data.permittedPaths || [],
        blocked: data.blockedPaths || []
      });
    }
    
    // Group permissions
    this.groupPermissions();
  }
  
  private groupPermissions(): void {
    const perms = this.permissions();
    this.filePermissions.set(perms.filter(p => p.category === 'file'));
    this.systemPermissions.set(perms.filter(p => p.category === 'system'));
    this.gitPermissions.set(perms.filter(p => p.category === 'git'));
    this.toolPermissions.set(perms.filter(p => p.category === 'tool'));
  }
  
  togglePermission(permission: SessionPermission): void {
    this.permissions.update(perms =>
      perms.map(p =>
        p.id === permission.id ? { ...p, granted: !p.granted } : p
      )
    );
    this.groupPermissions();
  }
  
  selectAll(category: 'file' | 'system' | 'git' | 'tool'): void {
    this.permissions.update(perms =>
      perms.map(p =>
        p.category === category ? { ...p, granted: true } : p
      )
    );
    this.groupPermissions();
  }
  
  deselectAll(category: 'file' | 'system' | 'git' | 'tool'): void {
    this.permissions.update(perms =>
      perms.map(p =>
        p.category === category ? { ...p, granted: false } : p
      )
    );
    this.groupPermissions();
  }
  
  addPermittedPath(): void {
    if (!this.newPermittedPath.trim()) return;
    
    this.sessionPaths.update(paths => ({
      ...paths,
      permitted: [...paths.permitted, this.newPermittedPath.trim()]
    }));
    
    this.newPermittedPath = '';
  }
  
  removePermittedPath(path: string): void {
    this.sessionPaths.update(paths => ({
      ...paths,
      permitted: paths.permitted.filter(p => p !== path)
    }));
  }
  
  addBlockedPath(): void {
    if (!this.newBlockedPath.trim()) return;
    
    this.sessionPaths.update(paths => ({
      ...paths,
      blocked: [...paths.blocked, this.newBlockedPath.trim()]
    }));
    
    this.newBlockedPath = '';
  }
  
  removeBlockedPath(path: string): void {
    this.sessionPaths.update(paths => ({
      ...paths,
      blocked: paths.blocked.filter(p => p !== path)
    }));
  }
  
  applyPermissions(): void {
    const result = {
      permissions: this.permissions(),
      paths: this.sessionPaths()
    };
    this.dialogRef.close(result);
  }
  
  cancel(): void {
    this.dialogRef.close();
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
}