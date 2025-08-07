import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ListItemComponent } from '../../shared/lists/list-item/list-item';
import { PanelComponent } from '../../shared/panels/panel/panel';
import { FormFieldComponent } from '../../shared/forms/form-field/form-field';

export interface Provider {
  id: string;
  name: string;
  type: 'openai' | 'claude' | 'ollama' | 'lmstudio';
  apiKey?: string;
  baseUrl?: string;
  model?: string;
  enabled: boolean;
}

@Component({
  selector: 'app-provider-config',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatTooltipModule,
    ListItemComponent,
    PanelComponent,
    FormFieldComponent
  ],
  templateUrl: './provider-config.html',
  styleUrl: './provider-config.scss'
})
export class ProviderConfigComponent {
  @Input() providers: Provider[] = [];
  @Input() availableModels: Record<string, string[]> = {};
  @Input() form!: FormGroup;
  
  @Output() providerSelected = new EventEmitter<Provider>();
  @Output() providerAdded = new EventEmitter<void>();
  @Output() providerSaved = new EventEmitter<Provider>();
  @Output() providerDeleted = new EventEmitter<Provider>();
  @Output() providerTested = new EventEmitter<Provider>();
  
  selectedProvider = signal<Provider | null>(null);
  editingProvider = signal(false);
  
  getProviderIcon(type: string): string {
    const icons: Record<string, string> = {
      'openai': 'auto_awesome',
      'claude': 'psychology',
      'ollama': 'computer',
      'lmstudio': 'desktop_windows'
    };
    return icons[type] || 'smart_toy';
  }
  
  selectProvider(provider: Provider) {
    this.selectedProvider.set(provider);
    this.editingProvider.set(false);
    this.providerSelected.emit(provider);
  }
  
  addNewProvider() {
    this.providerAdded.emit();
  }
  
  saveProvider() {
    if (this.selectedProvider()) {
      this.providerSaved.emit(this.selectedProvider()!);
      this.editingProvider.set(false);
    }
  }
  
  deleteProvider() {
    if (this.selectedProvider()) {
      this.providerDeleted.emit(this.selectedProvider()!);
    }
  }
  
  testProvider() {
    if (this.selectedProvider()) {
      this.providerTested.emit(this.selectedProvider()!);
    }
  }
  
  startEditing() {
    this.editingProvider.set(true);
  }
  
  cancelEditing() {
    this.editingProvider.set(false);
  }
}