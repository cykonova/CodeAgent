import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';

interface Provider {
  id: string;
  name: string;
  type: string;
  apiKey?: string;
  endpoint?: string;
  model?: string;
  enabled: boolean;
}

interface Agent {
  id: string;
  name: string;
  type: string;
  description: string;
  providerId: string;
  enabled: boolean;
  configuration: any;
}

interface SecuritySettings {
  sandboxLevel: 'none' | 'container' | 'vm';
  enableMcp: boolean;
  allowFileAccess: boolean;
  maxExecutionTime: number;
  allowNetworkAccess: boolean;
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatTabsModule,
    MatCardModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatDividerModule,
    MatListModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatSnackBarModule,
    MatDialogModule,
    MatChipsModule
  ],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  providerForm!: FormGroup;
  agentForm!: FormGroup;
  securityForm!: FormGroup;

  providers: Provider[] = [];
  agents: Agent[] = [];
  selectedProvider: Provider | null = null;
  selectedAgent: Agent | null = null;

  providerTypes = [
    { value: 'anthropic', label: 'Anthropic Claude' },
    { value: 'openai', label: 'OpenAI' },
    { value: 'ollama', label: 'Ollama (Local)' },
    { value: 'custom', label: 'Custom API' }
  ];

  agentTypes = [
    { value: 'general', label: 'General Assistant' },
    { value: 'code', label: 'Code Assistant' },
    { value: 'review', label: 'Code Reviewer' },
    { value: 'architect', label: 'System Architect' },
    { value: 'tester', label: 'Test Generator' }
  ];

  sandboxLevels = [
    { value: 'none', label: 'No Sandbox', description: 'Direct execution (not recommended)' },
    { value: 'container', label: 'Container', description: 'Docker container isolation' },
    { value: 'vm', label: 'Virtual Machine', description: 'Full VM isolation (most secure)' }
  ];

  constructor(
    private fb: FormBuilder,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.initializeForms();
    this.loadSettings();
  }

  private initializeForms(): void {
    this.providerForm = this.fb.group({
      name: ['', Validators.required],
      type: ['', Validators.required],
      apiKey: [''],
      endpoint: [''],
      model: [''],
      enabled: [true]
    });

    this.agentForm = this.fb.group({
      name: ['', Validators.required],
      type: ['', Validators.required],
      description: [''],
      providerId: ['', Validators.required],
      enabled: [true],
      temperature: [0.7],
      maxTokens: [4096],
      topP: [1],
      frequencyPenalty: [0],
      presencePenalty: [0]
    });

    this.securityForm = this.fb.group({
      sandboxLevel: ['container', Validators.required],
      enableMcp: [true],
      allowFileAccess: [false],
      maxExecutionTime: [300000],
      allowNetworkAccess: [false]
    });
  }

  private loadSettings(): void {
    this.providers = this.getStoredProviders();
    this.agents = this.getStoredAgents();
    this.loadSecuritySettings();
  }

  private getStoredProviders(): Provider[] {
    const stored = localStorage.getItem('providers');
    return stored ? JSON.parse(stored) : [];
  }

  private getStoredAgents(): Agent[] {
    const stored = localStorage.getItem('agents');
    return stored ? JSON.parse(stored) : [];
  }

  private loadSecuritySettings(): void {
    const stored = localStorage.getItem('securitySettings');
    if (stored) {
      this.securityForm.patchValue(JSON.parse(stored));
    }
  }

  addProvider(): void {
    if (this.providerForm.valid) {
      const provider: Provider = {
        id: this.generateId(),
        ...this.providerForm.value
      };
      this.providers.push(provider);
      this.saveProviders();
      this.providerForm.reset({ enabled: true });
      this.showSuccess('Provider added successfully');
    }
  }

  editProvider(provider: Provider): void {
    this.selectedProvider = provider;
    this.providerForm.patchValue(provider);
  }

  updateProvider(): void {
    if (this.selectedProvider && this.providerForm.valid) {
      const index = this.providers.findIndex(p => p.id === this.selectedProvider!.id);
      if (index !== -1) {
        this.providers[index] = {
          ...this.selectedProvider,
          ...this.providerForm.value
        };
        this.saveProviders();
        this.selectedProvider = null;
        this.providerForm.reset({ enabled: true });
        this.showSuccess('Provider updated successfully');
      }
    }
  }

  deleteProvider(provider: Provider): void {
    const index = this.providers.indexOf(provider);
    if (index !== -1) {
      this.providers.splice(index, 1);
      this.saveProviders();
      this.showSuccess('Provider deleted successfully');
    }
  }

  private saveProviders(): void {
    localStorage.setItem('providers', JSON.stringify(this.providers));
  }

  addAgent(): void {
    if (this.agentForm.valid) {
      const agent: Agent = {
        id: this.generateId(),
        name: this.agentForm.value.name,
        type: this.agentForm.value.type,
        description: this.agentForm.value.description,
        providerId: this.agentForm.value.providerId,
        enabled: this.agentForm.value.enabled,
        configuration: {
          temperature: this.agentForm.value.temperature,
          maxTokens: this.agentForm.value.maxTokens,
          topP: this.agentForm.value.topP,
          frequencyPenalty: this.agentForm.value.frequencyPenalty,
          presencePenalty: this.agentForm.value.presencePenalty
        }
      };
      this.agents.push(agent);
      this.saveAgents();
      this.agentForm.reset({ enabled: true, temperature: 0.7, maxTokens: 4096, topP: 1 });
      this.showSuccess('Agent added successfully');
    }
  }

  editAgent(agent: Agent): void {
    this.selectedAgent = agent;
    this.agentForm.patchValue({
      ...agent,
      ...agent.configuration
    });
  }

  updateAgent(): void {
    if (this.selectedAgent && this.agentForm.valid) {
      const index = this.agents.findIndex(a => a.id === this.selectedAgent!.id);
      if (index !== -1) {
        this.agents[index] = {
          ...this.selectedAgent,
          name: this.agentForm.value.name,
          type: this.agentForm.value.type,
          description: this.agentForm.value.description,
          providerId: this.agentForm.value.providerId,
          enabled: this.agentForm.value.enabled,
          configuration: {
            temperature: this.agentForm.value.temperature,
            maxTokens: this.agentForm.value.maxTokens,
            topP: this.agentForm.value.topP,
            frequencyPenalty: this.agentForm.value.frequencyPenalty,
            presencePenalty: this.agentForm.value.presencePenalty
          }
        };
        this.saveAgents();
        this.selectedAgent = null;
        this.agentForm.reset({ enabled: true, temperature: 0.7, maxTokens: 4096, topP: 1 });
        this.showSuccess('Agent updated successfully');
      }
    }
  }

  deleteAgent(agent: Agent): void {
    const index = this.agents.indexOf(agent);
    if (index !== -1) {
      this.agents.splice(index, 1);
      this.saveAgents();
      this.showSuccess('Agent deleted successfully');
    }
  }

  private saveAgents(): void {
    localStorage.setItem('agents', JSON.stringify(this.agents));
  }

  saveSecuritySettings(): void {
    if (this.securityForm.valid) {
      localStorage.setItem('securitySettings', JSON.stringify(this.securityForm.value));
      this.showSuccess('Security settings saved successfully');
    }
  }

  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }

  cancelProviderEdit(): void {
    this.selectedProvider = null;
    this.providerForm.reset({ enabled: true });
  }

  cancelAgentEdit(): void {
    this.selectedAgent = null;
    this.agentForm.reset({ enabled: true, temperature: 0.7, maxTokens: 4096, topP: 1 });
  }

  getProviderName(providerId: string): string {
    const provider = this.providers.find(p => p.id === providerId);
    return provider ? provider.name : 'Unknown';
  }
}