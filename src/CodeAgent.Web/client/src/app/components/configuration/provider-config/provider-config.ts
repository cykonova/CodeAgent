import { Component, Input, Output, EventEmitter, signal, inject, OnInit } from '@angular/core';
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
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { ListItemComponent } from '../../shared/lists/list-item/list-item';
import { PanelComponent } from '../../shared/panels/panel/panel';
import { FormFieldComponent } from '../../shared/forms/form-field/form-field';
import { ModelService } from '../../../services/model.service';

export interface Provider {
  id: string;
  name: string;
  type: 'openai' | 'claude' | 'ollama' | 'lmstudio' | 'docker' | 'docker-mcp';
  apiKey?: string;
  baseUrl?: string;
  model?: string;
  enabled: boolean;
  supportsModelManagement?: boolean;
}

export interface ModelInfo {
  id: string;
  name: string;
  description?: string;
  size?: number;
  isInstalled?: boolean;
  provider?: string;
  capabilities?: {
    supportsChat?: boolean;
    supportsTools?: boolean;
    supportsVision?: boolean;
    contextWindow?: number;
  };
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
    MatProgressBarModule,
    MatChipsModule,
    MatDialogModule,
    MatTabsModule,
    ListItemComponent,
    PanelComponent,
    FormFieldComponent
  ],
  templateUrl: './provider-config.html',
  styleUrl: './provider-config.scss'
})
export class ProviderConfigComponent implements OnInit {
  @Input() providers: Provider[] = [];
  @Input() availableModels: Record<string, string[]> = {};
  @Input() form!: FormGroup;
  
  @Output() providerSelected = new EventEmitter<Provider>();
  @Output() providerAdded = new EventEmitter<void>();
  @Output() providerSaved = new EventEmitter<Provider>();
  @Output() providerDeleted = new EventEmitter<Provider>();
  @Output() providerTested = new EventEmitter<Provider>();
  @Output() modelInstalled = new EventEmitter<{provider: Provider, model: ModelInfo}>();
  @Output() modelSelected = new EventEmitter<{provider: Provider, model: string}>();
  
  private modelService = inject(ModelService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  
  selectedProvider = signal<Provider | null>(null);
  editingProvider = signal(false);
  availableModelsList = signal<ModelInfo[]>([]);
  installedModels = signal<ModelInfo[]>([]);
  isLoadingModels = signal(false);
  searchQuery = signal('');
  installProgress = signal<number | null>(null);
  
  ngOnInit() {
    // Load models when a provider is selected
    this.loadModelsForProvider();
  }
  
  getProviderIcon(type: string): string {
    const icons: Record<string, string> = {
      'openai': 'auto_awesome',
      'claude': 'psychology',
      'ollama': 'computer',
      'lmstudio': 'desktop_windows',
      'docker': 'dock',
      'docker-mcp': 'hub'
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
  
  async loadModelsForProvider() {
    const provider = this.selectedProvider();
    if (!provider || !provider.supportsModelManagement) {
      return;
    }
    
    this.isLoadingModels.set(true);
    try {
      const models = await this.modelService.listModels(provider.id).toPromise();
      this.availableModelsList.set(models || []);
      this.installedModels.set(models?.filter(m => m.isInstalled) || []);
    } catch (error) {
      console.error('Failed to load models:', error);
      this.snackBar.open('Failed to load models', 'Close', { duration: 3000 });
    } finally {
      this.isLoadingModels.set(false);
    }
  }
  
  async searchModels() {
    const provider = this.selectedProvider();
    const query = this.searchQuery();
    
    if (!provider || !query) {
      return;
    }
    
    this.isLoadingModels.set(true);
    try {
      const models = await this.modelService.searchModels(provider.id, query).toPromise();
      this.availableModelsList.set(models || []);
    } catch (error) {
      console.error('Failed to search models:', error);
      this.snackBar.open('Failed to search models', 'Close', { duration: 3000 });
    } finally {
      this.isLoadingModels.set(false);
    }
  }
  
  async installModel(model: ModelInfo) {
    const provider = this.selectedProvider();
    if (!provider) return;
    
    this.installProgress.set(0);
    
    try {
      await this.modelService.installModel(provider.id, model.id, (progress) => {
        this.installProgress.set(progress.percentComplete);
      }).toPromise();
      
      this.snackBar.open(`Model ${model.name} installed successfully`, 'Close', { duration: 3000 });
      this.modelInstalled.emit({ provider, model });
      
      // Reload models
      await this.loadModelsForProvider();
    } catch (error) {
      console.error('Failed to install model:', error);
      this.snackBar.open(`Failed to install model: ${error}`, 'Close', { duration: 5000 });
    } finally {
      this.installProgress.set(null);
    }
  }
  
  async uninstallModel(model: ModelInfo) {
    const provider = this.selectedProvider();
    if (!provider) return;
    
    const confirmed = confirm(`Are you sure you want to uninstall ${model.name}?`);
    if (!confirmed) return;
    
    try {
      await this.modelService.uninstallModel(provider.id, model.id).toPromise();
      this.snackBar.open(`Model ${model.name} uninstalled`, 'Close', { duration: 3000 });
      
      // Reload models
      await this.loadModelsForProvider();
    } catch (error) {
      console.error('Failed to uninstall model:', error);
      this.snackBar.open(`Failed to uninstall model: ${error}`, 'Close', { duration: 5000 });
    }
  }
  
  selectModel(modelId: string) {
    const provider = this.selectedProvider();
    if (provider) {
      provider.model = modelId;
      this.modelSelected.emit({ provider, model: modelId });
      this.saveProvider();
    }
  }
  
  formatModelSize(bytes?: number): string {
    if (!bytes) return '';
    const gb = bytes / (1024 * 1024 * 1024);
    return gb > 1 ? `${gb.toFixed(1)} GB` : `${(bytes / (1024 * 1024)).toFixed(0)} MB`;
  }
  
  supportsModelManagement(provider: Provider | null): boolean {
    if (!provider) return false;
    return provider.type === 'ollama' || provider.type === 'docker' || provider.type === 'docker-mcp';
  }
}