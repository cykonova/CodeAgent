import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup } from '@angular/forms';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { ChipListComponent, ChipItem } from '../../shared/chips/chip-list/chip-list';
import { PanelComponent } from '../../shared/panels/panel/panel';

export interface SecurityConfig {
  allowFileRead: boolean;
  allowFileWrite: boolean;
  allowFileDelete: boolean;
  allowGitOperations: boolean;
  allowSystemCommands: boolean;
  requireConfirmation: boolean;
  permittedPaths: string[];
  blockedPaths: string[];
}

export interface Permission {
  id: string;
  label: string;
  icon: string;
  enabled: boolean;
}

@Component({
  selector: 'app-security-config',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    ChipListComponent,
    PanelComponent
  ],
  templateUrl: './security-config.html',
  styleUrl: './security-config.scss'
})
export class SecurityConfigComponent {
  @Input() form!: FormGroup;
  @Input() config!: SecurityConfig;
  
  @Output() configChange = new EventEmitter<SecurityConfig>();
  @Output() addPermittedPath = new EventEmitter<string>();
  @Output() removePermittedPath = new EventEmitter<string>();
  @Output() addBlockedPath = new EventEmitter<string>();
  @Output() removeBlockedPath = new EventEmitter<string>();
  
  newPermittedPath: string = '';
  newBlockedPath: string = '';
  
  permissions: Permission[] = [
    { id: 'fileRead', label: 'File Read', icon: 'visibility', enabled: false },
    { id: 'fileWrite', label: 'File Write', icon: 'edit', enabled: false },
    { id: 'fileDelete', label: 'File Delete', icon: 'delete', enabled: false },
    { id: 'gitOperations', label: 'Git Operations', icon: 'source', enabled: false },
    { id: 'systemCommands', label: 'System Commands', icon: 'terminal', enabled: false },
    { id: 'requireConfirmation', label: 'Require Confirmation', icon: 'verified_user', enabled: false }
  ];
  
  onAddPermittedPath() {
    if (this.newPermittedPath.trim()) {
      this.addPermittedPath.emit(this.newPermittedPath.trim());
      this.newPermittedPath = '';
    }
  }
  
  onAddBlockedPath() {
    if (this.newBlockedPath.trim()) {
      this.addBlockedPath.emit(this.newBlockedPath.trim());
      this.newBlockedPath = '';
    }
  }
  
  getPermittedPathChips(): ChipItem[] {
    return this.config.permittedPaths.map(path => ({
      id: path,
      label: path,
      icon: 'check_circle',
      color: 'primary',
      removable: true
    }));
  }
  
  getBlockedPathChips(): ChipItem[] {
    return this.config.blockedPaths.map(path => ({
      id: path,
      label: path,
      icon: 'block',
      color: 'warn',
      removable: true
    }));
  }
  
  onPermittedChipRemoved(chip: ChipItem) {
    this.removePermittedPath.emit(chip.id);
  }
  
  onBlockedChipRemoved(chip: ChipItem) {
    this.removeBlockedPath.emit(chip.id);
  }
}