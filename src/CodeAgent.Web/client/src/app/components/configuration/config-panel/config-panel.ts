import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ChatService } from '../../../services/chat.service';
import { AppCardComponent } from '../../shared/card/app-card/app-card';
import { GeneralConfigComponent } from '../general-config/general-config';
import { ProviderConfigComponent } from '../provider-config/provider-config';
import { SecurityConfigComponent } from '../security-config/security-config';

interface Provider {
  id: string;
  name: string;
  type: 'openai' | 'claude' | 'ollama' | 'lmstudio' | 'docker' | 'docker-mcp';
  apiKey?: string;
  baseUrl?: string;
  model?: string;
  enabled: boolean;
}

interface GeneralConfig {
  projectDirectory: string;
  additionalPermittedDirectories: string[];
  autoSave: boolean;
  confirmBeforeDelete: boolean;
  maxTokens: number;
  temperature: number;
  defaultProvider: string;
}

interface SecurityConfig {
  allowFileRead: boolean;
  allowFileWrite: boolean;
  allowFileDelete: boolean;
  allowGitOperations: boolean;
  allowSystemCommands: boolean;
  requireConfirmation: boolean;
  permittedPaths: string[];
  blockedPaths: string[];
}

@Component({
  selector: 'app-config-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDialogModule,
    AppCardComponent,
    GeneralConfigComponent,
    ProviderConfigComponent,
    SecurityConfigComponent
  ],
  templateUrl: './config-panel.html',
  styleUrl: './config-panel.scss'
})
export class ConfigPanel implements OnInit {
  generalForm: FormGroup;
  securityForm: FormGroup;
  
  // Card actions for the header
  cardActions = [
    { icon: 'upload_file', label: 'Import', action: 'import' },
    { icon: 'download', label: 'Export', action: 'export' },
    { icon: 'save', label: 'Save All', action: 'save', color: 'primary' }
  ];
  
  isSaving = signal(false);
  selectedTab = signal(0);
  selectedProvider = signal<Provider | null>(null);
  editingProvider = signal(false);
  
  providers = signal<Provider[]>([
    {
      id: 'openai-default',
      name: 'OpenAI GPT-4',
      type: 'openai',
      apiKey: '',
      baseUrl: 'https://api.openai.com/v1',
      model: 'gpt-4-turbo-preview',
      enabled: false
    },
    {
      id: 'claude-default',
      name: 'Claude 3 Sonnet',
      type: 'claude',
      apiKey: '',
      baseUrl: 'https://api.anthropic.com/v1',
      model: 'claude-3-sonnet-20240229',
      enabled: false
    },
    {
      id: 'ollama-default',
      name: 'Ollama Local',
      type: 'ollama',
      baseUrl: 'http://localhost:11434',
      model: 'llama2',
      enabled: true
    }
  ]);
  
  generalConfig = signal<GeneralConfig>({
    projectDirectory: '',
    additionalPermittedDirectories: [],
    autoSave: true,
    confirmBeforeDelete: true,
    maxTokens: 4096,
    temperature: 0.7,
    defaultProvider: 'ollama-default'
  });
  
  securityConfig = signal<SecurityConfig>({
    allowFileRead: true,
    allowFileWrite: true,
    allowFileDelete: false,
    allowGitOperations: true,
    allowSystemCommands: false,
    requireConfirmation: true,
    permittedPaths: [],
    blockedPaths: []
  });
  
  availableModels: { [key: string]: string[] } = {
    openai: ['gpt-4-turbo-preview', 'gpt-4', 'gpt-3.5-turbo'],
    claude: ['claude-3-opus-20240229', 'claude-3-sonnet-20240229', 'claude-3-haiku-20240307'],
    ollama: ['llama2', 'codellama', 'mistral', 'mixtral'],
    lmstudio: ['custom'],
    docker: ['llama2', 'codellama', 'mistral', 'mixtral'],
    'docker-mcp': ['docker-mcp']
  };
  
  // Provider form for editing
  providerForm: FormGroup;
  
  // For adding new directories
  newPermittedDir = '';
  newPermittedPath = '';
  newBlockedPath = '';
  
  constructor(
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private chatService: ChatService
  ) {
    // Initialize general settings form
    this.generalForm = this.fb.group({
      projectDirectory: ['', Validators.required],
      autoSave: [true],
      confirmBeforeDelete: [true],
      maxTokens: [4096, [Validators.required, Validators.min(100), Validators.max(32000)]],
      temperature: [0.7, [Validators.required, Validators.min(0), Validators.max(2)]],
      defaultProvider: ['ollama-default', Validators.required]
    });
    
    // Initialize security form
    this.securityForm = this.fb.group({
      allowFileRead: [true],
      allowFileWrite: [true],
      allowFileDelete: [false],
      allowGitOperations: [true],
      allowSystemCommands: [false],
      requireConfirmation: [true]
    });
    
    // Initialize provider form
    this.providerForm = this.fb.group({
      name: ['', Validators.required],
      type: ['openai', Validators.required],
      apiKey: [''],
      baseUrl: [''],
      model: ['', Validators.required],
      enabled: [true]
    });
  }
  
  ngOnInit(): void {
    this.loadConfiguration();
    this.setupFormValidation();
  }
  
  private setupFormValidation(): void {
    // Update validation based on provider type
    this.providerForm.get('type')?.valueChanges.subscribe(type => {
      const apiKeyControl = this.providerForm.get('apiKey');
      const baseUrlControl = this.providerForm.get('baseUrl');
      const modelControl = this.providerForm.get('model');
      const nameControl = this.providerForm.get('name');
      
      // Set default URLs and models based on provider type
      const defaults = this.getProviderDefaults(type);
      
      if (type === 'openai' || type === 'claude') {
        apiKeyControl?.setValidators([Validators.required]);
        baseUrlControl?.clearValidators();
        baseUrlControl?.setValue(defaults.baseUrl || '');
      } else if (type === 'ollama' || type === 'lmstudio' || type === 'docker' || type === 'docker-mcp') {
        apiKeyControl?.clearValidators();
        baseUrlControl?.setValidators([Validators.required]);
        baseUrlControl?.setValue(defaults.baseUrl || '');
        apiKeyControl?.setValue('');
      }
      
      // Set default model if none is selected
      if (!modelControl?.value || this.editingProvider()) {
        modelControl?.setValue(defaults.model);
      }
      
      // Update name to default if it's currently a default name
      const currentName = nameControl?.value;
      const isDefaultName = currentName === 'New Provider' || 
                           currentName === 'OpenAI GPT-4' || 
                           currentName === 'Claude 3 Sonnet' || 
                           currentName === 'Ollama Local' || 
                           currentName === 'LM Studio Local';
      
      if (this.editingProvider() && isDefaultName) {
        nameControl?.setValue(defaults.name);
      }
      
      apiKeyControl?.updateValueAndValidity();
      baseUrlControl?.updateValueAndValidity();
    });
  }
  
  private getProviderDefaults(type: string): { baseUrl?: string; model: string; name: string } {
    switch (type) {
      case 'openai':
        return {
          baseUrl: 'https://api.openai.com/v1',
          model: 'gpt-4-turbo-preview',
          name: 'OpenAI GPT-4'
        };
      case 'claude':
        return {
          baseUrl: 'https://api.anthropic.com/v1',
          model: 'claude-3-sonnet-20240229',
          name: 'Claude 3 Sonnet'
        };
      case 'ollama':
        return {
          baseUrl: 'http://localhost:11434',
          model: 'llama2',
          name: 'Ollama Local'
        };
      case 'lmstudio':
        return {
          baseUrl: 'http://localhost:1234/v1',
          model: 'local-model',
          name: 'LM Studio Local'
        };
      case 'docker':
        return {
          baseUrl: 'http://localhost:2375',
          model: 'llama2',
          name: 'Docker LLM'
        };
      case 'docker-mcp':
        return {
          baseUrl: 'http://localhost:2376',
          model: 'docker-mcp',
          name: 'Docker MCP'
        };
      default:
        return {
          model: 'gpt-4-turbo-preview',
          name: 'New Provider'
        };
    }
  }
  
  private loadConfiguration(): void {
    // Load configuration from API
    this.chatService.getConfiguration().subscribe({
      next: (config) => {
        if (config.defaultProvider) {
          this.generalForm.patchValue({ defaultProvider: config.defaultProvider });
        }
        
        // Update providers based on API response
        if (config.providers) {
          const loadedProviders: Provider[] = [];
          
          // OpenAI provider
          if (config.providers.openAI) {
            loadedProviders.push({
              id: 'openai',
              name: 'OpenAI',
              type: 'openai',
              apiKey: config.providers.openAI.apiKeySet ? '***' : '',
              model: config.providers.openAI.model || 'gpt-4',
              enabled: config.providers.openAI.apiKeySet
            });
          }
          
          // Claude provider
          if (config.providers.claude) {
            loadedProviders.push({
              id: 'claude',
              name: 'Claude',
              type: 'claude',
              apiKey: config.providers.claude.apiKeySet ? '***' : '',
              model: config.providers.claude.model || 'claude-3-sonnet-20240229',
              enabled: config.providers.claude.apiKeySet
            });
          }
          
          // Ollama provider
          if (config.providers.ollama) {
            loadedProviders.push({
              id: 'ollama',
              name: 'Ollama',
              type: 'ollama',
              baseUrl: config.providers.ollama.baseUrl || 'http://localhost:11434',
              model: config.providers.ollama.model || 'llama2',
              enabled: true
            });
          }
          
          this.providers.set(loadedProviders);
        }
        
        // Load available providers
        this.loadAvailableProviders();
      },
      error: (error) => {
        console.error('Failed to load configuration:', error);
        this.snackBar.open('Failed to load configuration', 'Close', {
          duration: 3000
        });
      }
    });
  }
  
  private loadAvailableProviders(): void {
    this.chatService.getProviders().subscribe({
      next: (providers) => {
        console.log('Available providers:', providers);
      },
      error: (error) => {
        console.error('Failed to load providers:', error);
      }
    });
  }
  
  // Provider Management
  selectProvider(provider: Provider): void {
    this.selectedProvider.set(provider);
    this.providerForm.patchValue({
      name: provider.name,
      type: provider.type,
      apiKey: provider.apiKey || '',
      baseUrl: provider.baseUrl || '',
      model: provider.model || '',
      enabled: provider.enabled
    });
    this.editingProvider.set(false);
  }
  
  addNewProvider(): void {
    const defaults = this.getProviderDefaults('openai');
    const newProvider: Provider = {
      id: `provider-${Date.now()}`,
      name: defaults.name,
      type: 'openai',
      apiKey: '',
      baseUrl: defaults.baseUrl,
      model: defaults.model,
      enabled: false
    };
    
    this.providers.update(providers => [...providers, newProvider]);
    this.selectProvider(newProvider);
    this.editingProvider.set(true);
  }
  
  saveProvider(): void {
    if (!this.providerForm.valid) {
      this.snackBar.open('Please fill in all required fields', 'Close', {
        duration: 3000
      });
      return;
    }
    
    const currentProvider = this.selectedProvider();
    if (!currentProvider) return;
    
    const formValue = this.providerForm.value;
    
    this.providers.update(providers => 
      providers.map(p => 
        p.id === currentProvider.id 
          ? { ...p, ...formValue }
          : p
      )
    );
    
    this.editingProvider.set(false);
    this.snackBar.open('Provider saved', 'Close', { duration: 2000 });
  }
  
  deleteProvider(): void {
    const currentProvider = this.selectedProvider();
    if (!currentProvider) return;
    
    if (this.generalConfig().confirmBeforeDelete) {
      if (!confirm(`Are you sure you want to delete ${currentProvider.name}?`)) {
        return;
      }
    }
    
    this.providers.update(providers => 
      providers.filter(p => p.id !== currentProvider.id)
    );
    
    this.selectedProvider.set(null);
    this.snackBar.open('Provider deleted', 'Close', { duration: 2000 });
  }
  
  testProvider(): void {
    const currentProvider = this.selectedProvider();
    if (!currentProvider) return;
    
    this.snackBar.open(`Testing connection to ${currentProvider.name}...`, 'Close', {
      duration: 3000
    });
    
    // Send test message to verify provider works
    this.chatService.sendMessage({
      message: 'Hello, this is a test message.',
      provider: currentProvider.id,
      model: currentProvider.model
    }).subscribe({
      next: (response) => {
        this.snackBar.open(`✓ ${currentProvider.name} connection successful`, 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
      },
      error: (error) => {
        this.snackBar.open(`✗ ${currentProvider.name} connection failed: ${error.message}`, 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }
  
  // Card action handler
  onCardAction(action: string): void {
    switch (action) {
      case 'import':
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json';
        input.onchange = (e: any) => this.importConfiguration(e);
        input.click();
        break;
      case 'export':
        this.exportConfiguration();
        break;
      case 'save':
        this.saveConfiguration();
        break;
    }
  }
  
  // Directory Management
  addPermittedDirectory(dir?: string): void {
    const directoryToAdd = dir || this.newPermittedDir.trim();
    if (!directoryToAdd) return;
    
    this.generalConfig.update(config => ({
      ...config,
      additionalPermittedDirectories: [
        ...config.additionalPermittedDirectories,
        directoryToAdd
      ]
    }));
    
    if (!dir) {
      this.newPermittedDir = '';
    }
  }
  
  removePermittedDirectory(dir: string): void {
    this.generalConfig.update(config => ({
      ...config,
      additionalPermittedDirectories: config.additionalPermittedDirectories.filter(d => d !== dir)
    }));
  }
  
  // Security Path Management
  addPermittedPath(path?: string): void {
    const pathToAdd = path || this.newPermittedPath.trim();
    if (!pathToAdd) return;
    
    this.securityConfig.update(config => ({
      ...config,
      permittedPaths: [...config.permittedPaths, pathToAdd]
    }));
    
    if (!path) {
      this.newPermittedPath = '';
    }
  }
  
  removePermittedPath(path: string): void {
    this.securityConfig.update(config => ({
      ...config,
      permittedPaths: config.permittedPaths.filter(p => p !== path)
    }));
  }
  
  addBlockedPath(path?: string): void {
    const pathToAdd = path || this.newBlockedPath.trim();
    if (!pathToAdd) return;
    
    this.securityConfig.update(config => ({
      ...config,
      blockedPaths: [...config.blockedPaths, pathToAdd]
    }));
    
    if (!path) {
      this.newBlockedPath = '';
    }
  }
  
  removeBlockedPath(path: string): void {
    this.securityConfig.update(config => ({
      ...config,
      blockedPaths: config.blockedPaths.filter(p => p !== path)
    }));
  }
  
  // Save Configuration
  saveConfiguration(): void {
    if (!this.generalForm.valid) {
      this.snackBar.open('Please fix validation errors in General settings', 'Close', {
        duration: 3000
      });
      return;
    }
    
    this.isSaving.set(true);
    
    // Update configurations
    this.generalConfig.update(config => ({
      ...config,
      ...this.generalForm.value
    }));
    
    this.securityConfig.update(config => ({
      ...config,
      ...this.securityForm.value
    }));
    
    // Build configuration object for API
    const configToSave = {
      defaultProvider: this.generalForm.value.defaultProvider,
      openAI: this.getProviderConfig('openai'),
      claude: this.getProviderConfig('claude'),
      ollama: this.getProviderConfig('ollama'),
      systemPrompt: '' // Could be added later
    };

    // Save configuration via API
    this.chatService.updateConfiguration(configToSave).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.snackBar.open('Configuration saved successfully', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
      },
      error: (error) => {
        this.isSaving.set(false);
        this.snackBar.open('Failed to save configuration', 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }
  
  private getProviderConfig(type: string): any {
    const provider = this.providers().find(p => p.type === type);
    if (!provider) return null;
    
    const config: any = {
      model: provider.model
    };
    
    if (provider.apiKey && provider.apiKey !== '***') {
      config.apiKey = provider.apiKey;
    }
    
    if (provider.baseUrl) {
      config.baseUrl = provider.baseUrl;
    }
    
    return config;
  }
  
  // Export/Import
  exportConfiguration(): void {
    const config = {
      general: this.generalConfig(),
      providers: this.providers(),
      security: this.securityConfig()
    };
    
    const blob = new Blob([JSON.stringify(config, null, 2)], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'codeagent-config.json';
    link.click();
    window.URL.revokeObjectURL(url);
    
    this.snackBar.open('Configuration exported', 'Close', {
      duration: 2000
    });
  }
  
  importConfiguration(event: any): void {
    const file = event.target.files[0];
    if (!file) return;
    
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const config = JSON.parse(e.target?.result as string);
        
        if (config.general) {
          this.generalConfig.set(config.general);
          this.generalForm.patchValue(config.general);
        }
        
        if (config.providers) {
          this.providers.set(config.providers);
        }
        
        if (config.security) {
          this.securityConfig.set(config.security);
          this.securityForm.patchValue(config.security);
        }
        
        this.snackBar.open('Configuration imported successfully', 'Close', {
          duration: 3000
        });
      } catch (error) {
        this.snackBar.open('Failed to import configuration', 'Close', {
          duration: 3000
        });
      }
    };
    
    reader.readAsText(file);
  }
  
  getProviderIcon(type: string): string {
    switch (type) {
      case 'openai': return 'auto_awesome';
      case 'claude': return 'psychology';
      case 'ollama': return 'computer';
      case 'lmstudio': return 'desktop_windows';
      case 'docker': return 'developer_board';
      case 'docker-mcp': return 'hub';
      default: return 'extension';
    }
  }
}