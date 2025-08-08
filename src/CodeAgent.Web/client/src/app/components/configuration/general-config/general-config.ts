import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ChipListComponent, ChipItem } from '../../shared/chips/chip-list/chip-list';
import { FormFieldComponent } from '../../shared/forms/form-field/form-field';
import { PanelComponent } from '../../shared/panels/panel/panel';

export interface GeneralConfig {
  projectDirectory: string;
  additionalPermittedDirectories: string[];
  defaultProvider: string;
  maxTokens: number;
  temperature: number;
  autoSave: boolean;
  confirmBeforeDelete: boolean;
}

export interface Provider {
  id: string;
  name: string;
  type: string;
  enabled: boolean;
}

@Component({
  selector: 'app-general-config',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatDividerModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    ChipListComponent,
    FormFieldComponent,
    PanelComponent
  ],
  templateUrl: './general-config.html',
  styleUrl: './general-config.scss'
})
export class GeneralConfigComponent {
  @Input() form!: FormGroup;
  @Input() config!: GeneralConfig;
  @Input() providers: Provider[] = [];
  
  @Output() configChange = new EventEmitter<GeneralConfig>();
  @Output() addDirectory = new EventEmitter<string>();
  @Output() removeDirectory = new EventEmitter<string>();
  
  newPermittedDir: string = '';
  
  getProviderIcon(type: string): string {
    const icons: Record<string, string> = {
      'openai': 'auto_awesome',
      'claude': 'psychology',
      'ollama': 'computer',
      'lmstudio': 'desktop_windows'
    };
    return icons[type] || 'smart_toy';
  }
  
  addPermittedDirectory() {
    if (this.newPermittedDir.trim()) {
      this.addDirectory.emit(this.newPermittedDir.trim());
      this.newPermittedDir = '';
    }
  }
  
  getDirectoryChips(): ChipItem[] {
    return this.config.additionalPermittedDirectories.map(dir => ({
      id: dir,
      label: dir,
      icon: 'folder_open',
      removable: true
    }));
  }
  
  onChipRemoved(chip: ChipItem) {
    this.removeDirectory.emit(chip.id);
  }
  
  onFolderSelected(event: Event, type: 'project' | 'additional') {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      // Get the path from the first file's webkitRelativePath
      const file = input.files[0];
      const path = (file as any).webkitRelativePath;
      
      if (path) {
        // Extract the folder path (remove the file name)
        const folderPath = path.substring(0, path.indexOf('/'));
        
        if (type === 'project') {
          // Update the project directory form control
          this.form.get('projectDirectory')?.setValue('/' + folderPath);
        } else {
          // Set the additional directory input
          this.newPermittedDir = '/' + folderPath;
        }
      }
    }
    
    // Reset the input so the same folder can be selected again
    input.value = '';
  }
}