# Phase 6c: Remote Applications

## Overview
Create the four remote micro-frontend applications (Dashboard, Projects, Chat, Settings) that will be loaded dynamically by the shell application via Module Federation.

## Visual References
Complete MockML mockups for each remote application:
- **Dashboard**: [`overview.mml`](../mockups/dashboard/overview.mml), [`metrics-detail.mml`](../mockups/dashboard/metrics-detail.mml)
- **Projects**: [`projects-list.mml`](../mockups/projects/projects-list.mml), [`project-detail.mml`](../mockups/projects/project-detail.mml), [`project-create.mml`](../mockups/projects/project-create.mml)
- **Chat**: [`chat-interface.mml`](../mockups/chat/chat-interface.mml), [`agent-selection.mml`](../mockups/chat/agent-selection.mml)
- **Settings**: [`general-settings.mml`](../mockups/settings/general-settings.mml), [`profile-settings.mml`](../mockups/settings/profile-settings.mml)

## Objectives
- Generate remote applications with Module Federation
- Implement Dashboard for metrics and status
- Create Projects interface for project management
- Build Chat interface for agent interactions
- Develop Settings for configuration management

## Remote Applications Setup

### 1. Generate Remote Applications
```bash
# Dashboard Remote
nx g @nx/angular:remote dashboard --host=shell \
  --standalone --style=scss

# Projects Remote  
nx g @nx/angular:remote projects --host=shell \
  --standalone --style=scss

# Chat Remote
nx g @nx/angular:remote chat --host=shell \
  --standalone --style=scss

# Settings Remote
nx g @nx/angular:remote settings --host=shell \
  --standalone --style=scss
```

## Dashboard Remote Application

### Dashboard Overview Component
```typescript
// apps/dashboard/src/app/dashboard/dashboard.component.ts
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressBarModule,
    UiCardsModule,
    UiChartsModule
  ],
  template: `
    <div class="dashboard-container">
      <h1>System Overview</h1>
      
      <div class="metrics-grid">
        <mat-card class="metric-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>memory</mat-icon>
            <mat-card-title>Active Agents</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="metric-value">{{ activeAgents }}</div>
            <mat-progress-bar mode="determinate" [value]="agentUtilization"></mat-progress-bar>
          </mat-card-content>
        </mat-card>
        
        <mat-card class="metric-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>api</mat-icon>
            <mat-card-title>API Calls</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="metric-value">{{ apiCalls | number }}</div>
            <div class="metric-subtitle">Last 24 hours</div>
          </mat-card-content>
        </mat-card>
        
        <mat-card class="metric-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>speed</mat-icon>
            <mat-card-title>Avg Response Time</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="metric-value">{{ responseTime }}ms</div>
            <ui-sparkline [data]="responseTimeHistory"></ui-sparkline>
          </mat-card-content>
        </mat-card>
        
        <mat-card class="metric-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>cloud</mat-icon>
            <mat-card-title>Providers</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="provider-list">
              <div *ngFor="let provider of providers" class="provider-item">
                <mat-icon [class.active]="provider.active">circle</mat-icon>
                {{ provider.name }}
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
      
      <div class="charts-section">
        <mat-card>
          <mat-card-header>
            <mat-card-title>Activity Timeline</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <ui-line-chart [data]="activityData"></ui-line-chart>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 20px;
    }
    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 20px;
      margin-bottom: 30px;
    }
    .metric-value {
      font-size: 2em;
      font-weight: bold;
      margin: 10px 0;
    }
    .metric-subtitle {
      color: rgba(0,0,0,0.6);
      font-size: 0.9em;
    }
    .provider-item {
      display: flex;
      align-items: center;
      gap: 8px;
      margin: 5px 0;
    }
    .provider-item mat-icon {
      font-size: 12px;
      width: 12px;
      height: 12px;
      color: #ccc;
    }
    .provider-item mat-icon.active {
      color: #4caf50;
    }
  `]
})
export class DashboardComponent implements OnInit {
  activeAgents = 0;
  agentUtilization = 0;
  apiCalls = 0;
  responseTime = 0;
  responseTimeHistory: number[] = [];
  providers: any[] = [];
  activityData: any;
  
  constructor(
    private dashboardService: DashboardService,
    private webSocket: WebSocketService
  ) {}
  
  ngOnInit() {
    this.loadMetrics();
    this.subscribeToUpdates();
  }
  
  private loadMetrics() {
    this.dashboardService.getMetrics().subscribe(metrics => {
      this.activeAgents = metrics.activeAgents;
      this.agentUtilization = metrics.agentUtilization;
      this.apiCalls = metrics.apiCalls;
      this.responseTime = metrics.avgResponseTime;
      this.responseTimeHistory = metrics.responseTimeHistory;
      this.providers = metrics.providers;
      this.activityData = metrics.activityData;
    });
  }
  
  private subscribeToUpdates() {
    this.webSocket.messages$.pipe(
      filter(msg => msg.type === 'metrics-update')
    ).subscribe(msg => {
      this.updateMetrics(msg.payload);
    });
  }
  
  private updateMetrics(updates: any) {
    // Update metrics with real-time data
  }
}
```

## Projects Remote Application

### Projects List Component
```typescript
// apps/projects/src/app/projects/projects-list.component.ts
@Component({
  selector: 'app-projects-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatMenuModule,
    UiTablesModule,
    RouterModule
  ],
  template: `
    <div class="projects-container">
      <div class="header">
        <h1>Projects</h1>
        <button mat-raised-button color="primary" (click)="createProject()">
          <mat-icon>add</mat-icon>
          New Project
        </button>
      </div>
      
      <ui-data-table [dataSource]="dataSource" 
                     [displayedColumns]="displayedColumns"
                     [clickable]="true"
                     (rowClick)="openProject($event)">
        
        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef mat-sort-header>Name</th>
          <td mat-cell *matCellDef="let project">
            <div class="project-name">
              <mat-icon>folder</mat-icon>
              {{ project.name }}
            </div>
          </td>
        </ng-container>
        
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let project">
            <mat-chip [color]="getStatusColor(project.status)">
              {{ project.status }}
            </mat-chip>
          </td>
        </ng-container>
        
        <ng-container matColumnDef="lastModified">
          <th mat-header-cell *matHeaderCellDef mat-sort-header>Last Modified</th>
          <td mat-cell *matCellDef="let project">
            {{ project.lastModified | date:'short' }}
          </td>
        </ng-container>
        
        <ng-container matColumnDef="agents">
          <th mat-header-cell *matHeaderCellDef>Agents</th>
          <td mat-cell *matCellDef="let project">
            <mat-chip-list>
              <mat-chip *ngFor="let agent of project.agents">
                {{ agent }}
              </mat-chip>
            </mat-chip-list>
          </td>
        </ng-container>
        
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let project">
            <button mat-icon-button [matMenuTriggerFor]="menu">
              <mat-icon>more_vert</mat-icon>
            </button>
            <mat-menu #menu="matMenu">
              <button mat-menu-item (click)="editProject(project)">
                <mat-icon>edit</mat-icon>
                <span>Edit</span>
              </button>
              <button mat-menu-item (click)="duplicateProject(project)">
                <mat-icon>content_copy</mat-icon>
                <span>Duplicate</span>
              </button>
              <button mat-menu-item (click)="deleteProject(project)">
                <mat-icon>delete</mat-icon>
                <span>Delete</span>
              </button>
            </mat-menu>
          </td>
        </ng-container>
      </ui-data-table>
    </div>
  `,
  styles: [`
    .projects-container {
      padding: 20px;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .project-name {
      display: flex;
      align-items: center;
      gap: 8px;
    }
  `]
})
export class ProjectsListComponent implements OnInit {
  dataSource = new MatTableDataSource<Project>();
  displayedColumns = ['name', 'status', 'lastModified', 'agents', 'actions'];
  
  constructor(
    private projectService: ProjectService,
    private router: Router,
    private dialog: MatDialog
  ) {}
  
  ngOnInit() {
    this.loadProjects();
  }
  
  loadProjects() {
    this.projectService.getProjects().subscribe(projects => {
      this.dataSource.data = projects;
    });
  }
  
  createProject() {
    const dialogRef = this.dialog.open(ProjectDialogComponent, {
      width: '600px',
      data: { mode: 'create' }
    });
    
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadProjects();
      }
    });
  }
  
  openProject(project: Project) {
    this.router.navigate(['/projects', project.id]);
  }
  
  editProject(project: Project) {
    // Implementation
  }
  
  duplicateProject(project: Project) {
    // Implementation
  }
  
  deleteProject(project: Project) {
    // Implementation
  }
  
  getStatusColor(status: string): string {
    switch(status) {
      case 'active': return 'primary';
      case 'paused': return 'warn';
      case 'completed': return 'accent';
      default: return '';
    }
  }
}
```

## Chat Remote Application

### Chat Interface Component
```typescript
// apps/chat/src/app/chat/chat.component.ts
@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="chat-container">
      <mat-card class="agent-selector">
        <mat-card-header>
          <mat-card-title>Active Agents</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <mat-chip-list>
            <mat-chip *ngFor="let agent of agents" 
                      [selected]="selectedAgent?.id === agent.id"
                      (click)="selectAgent(agent)">
              <mat-icon>smart_toy</mat-icon>
              {{ agent.name }}
            </mat-chip>
          </mat-chip-list>
        </mat-card-content>
      </mat-card>
      
      <mat-card class="chat-panel">
        <mat-card-content class="messages-container">
          <div class="messages" #scrollContainer>
            <div *ngFor="let message of messages" 
                 class="message"
                 [class.user]="message.sender === 'user'"
                 [class.agent]="message.sender === 'agent'">
              <div class="message-header">
                <mat-icon>{{ message.sender === 'user' ? 'person' : 'smart_toy' }}</mat-icon>
                <span class="sender">{{ message.senderName }}</span>
                <span class="timestamp">{{ message.timestamp | date:'short' }}</span>
              </div>
              <div class="message-content" [innerHTML]="message.content"></div>
            </div>
            
            <div *ngIf="isTyping" class="typing-indicator">
              <mat-spinner diameter="20"></mat-spinner>
              <span>Agent is typing...</span>
            </div>
          </div>
        </mat-card-content>
        
        <mat-card-actions class="input-area">
          <mat-form-field appearance="outline" class="message-input">
            <mat-label>Type your message</mat-label>
            <textarea matInput 
                      [(ngModel)]="messageText"
                      (keydown.enter)="sendMessage($event)"
                      [disabled]="!selectedAgent"
                      rows="3"></textarea>
            <mat-hint>Press Enter to send, Shift+Enter for new line</mat-hint>
          </mat-form-field>
          
          <button mat-fab color="primary" 
                  (click)="sendMessage()"
                  [disabled]="!messageText || !selectedAgent">
            <mat-icon>send</mat-icon>
          </button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .chat-container {
      display: grid;
      grid-template-columns: 300px 1fr;
      gap: 20px;
      height: calc(100vh - 100px);
      padding: 20px;
    }
    .agent-selector {
      height: fit-content;
    }
    .chat-panel {
      display: flex;
      flex-direction: column;
      height: 100%;
    }
    .messages-container {
      flex: 1;
      overflow-y: auto;
      padding: 20px;
    }
    .message {
      margin-bottom: 20px;
      padding: 10px;
      border-radius: 8px;
    }
    .message.user {
      background-color: #e3f2fd;
      margin-left: 20%;
    }
    .message.agent {
      background-color: #f5f5f5;
      margin-right: 20%;
    }
    .message-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;
      font-size: 0.9em;
      color: #666;
    }
    .message-content {
      white-space: pre-wrap;
    }
    .typing-indicator {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 10px;
    }
    .input-area {
      display: flex;
      gap: 10px;
      padding: 20px;
      border-top: 1px solid #e0e0e0;
    }
    .message-input {
      flex: 1;
    }
  `]
})
export class ChatComponent implements OnInit, AfterViewChecked {
  agents: Agent[] = [];
  selectedAgent: Agent | null = null;
  messages: ChatMessage[] = [];
  messageText = '';
  isTyping = false;
  
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;
  
  constructor(
    private agentService: AgentService,
    private chatService: ChatService,
    private webSocket: WebSocketService
  ) {}
  
  ngOnInit() {
    this.loadAgents();
    this.subscribeToMessages();
  }
  
  ngAfterViewChecked() {
    this.scrollToBottom();
  }
  
  loadAgents() {
    this.agentService.getActiveAgents().subscribe(agents => {
      this.agents = agents;
      if (agents.length > 0 && !this.selectedAgent) {
        this.selectAgent(agents[0]);
      }
    });
  }
  
  selectAgent(agent: Agent) {
    this.selectedAgent = agent;
    this.loadChatHistory(agent.id);
  }
  
  loadChatHistory(agentId: string) {
    this.chatService.getChatHistory(agentId).subscribe(messages => {
      this.messages = messages;
    });
  }
  
  sendMessage(event?: KeyboardEvent) {
    if (event && event.shiftKey) {
      return;
    }
    
    if (event) {
      event.preventDefault();
    }
    
    if (!this.messageText.trim() || !this.selectedAgent) {
      return;
    }
    
    const message: ChatMessage = {
      id: Date.now().toString(),
      sender: 'user',
      senderName: 'You',
      content: this.messageText,
      timestamp: new Date(),
      agentId: this.selectedAgent.id
    };
    
    this.messages.push(message);
    this.chatService.sendMessage(message).subscribe();
    
    this.messageText = '';
    this.isTyping = true;
  }
  
  private subscribeToMessages() {
    this.webSocket.messages$.pipe(
      filter(msg => msg.type === 'chat-message')
    ).subscribe(msg => {
      this.messages.push(msg.payload);
      this.isTyping = false;
    });
  }
  
  private scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = 
        this.scrollContainer.nativeElement.scrollHeight;
    } catch(err) {}
  }
}
```

## Settings Remote Application

### Settings Component
```typescript
// apps/settings/src/app/settings/settings.component.ts
@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTabsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatIconModule,
    UiFormsModule
  ],
  template: `
    <div class="settings-container">
      <h1>Settings</h1>
      
      <mat-tab-group>
        <mat-tab label="General">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Application Settings</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <form [formGroup]="generalForm">
                  <ui-text-input 
                    [control]="generalForm.get('apiUrl')!"
                    label="API URL"
                    hint="Backend API endpoint">
                  </ui-text-input>
                  
                  <mat-form-field appearance="outline">
                    <mat-label>Theme</mat-label>
                    <mat-select formControlName="theme">
                      <mat-option value="light">Light</mat-option>
                      <mat-option value="dark">Dark</mat-option>
                      <mat-option value="auto">Auto</mat-option>
                    </mat-select>
                  </mat-form-field>
                  
                  <mat-form-field appearance="outline">
                    <mat-label>Language</mat-label>
                    <mat-select formControlName="language">
                      <mat-option value="en">English</mat-option>
                      <mat-option value="es">Spanish</mat-option>
                      <mat-option value="fr">French</mat-option>
                      <mat-option value="de">German</mat-option>
                    </mat-select>
                  </mat-form-field>
                  
                  <mat-slide-toggle formControlName="notifications">
                    Enable notifications
                  </mat-slide-toggle>
                </form>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" (click)="saveGeneral()">
                  Save Changes
                </button>
              </mat-card-actions>
            </mat-card>
          </div>
        </mat-tab>
        
        <mat-tab label="Providers">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>LLM Providers</mat-card-title>
                <button mat-icon-button (click)="addProvider()">
                  <mat-icon>add</mat-icon>
                </button>
              </mat-card-header>
              <mat-card-content>
                <div class="provider-list">
                  <div *ngFor="let provider of providers" class="provider-card">
                    <div class="provider-header">
                      <h3>{{ provider.name }}</h3>
                      <mat-slide-toggle [(ngModel)]="provider.enabled">
                        Enabled
                      </mat-slide-toggle>
                    </div>
                    <div class="provider-config">
                      <mat-form-field appearance="outline">
                        <mat-label>API Key</mat-label>
                        <input matInput [type]="showKey ? 'text' : 'password'" 
                               [(ngModel)]="provider.apiKey">
                        <button mat-icon-button matSuffix (click)="showKey = !showKey">
                          <mat-icon>{{ showKey ? 'visibility_off' : 'visibility' }}</mat-icon>
                        </button>
                      </mat-form-field>
                      
                      <mat-form-field appearance="outline" *ngIf="provider.type === 'openai'">
                        <mat-label>Model</mat-label>
                        <mat-select [(ngModel)]="provider.model">
                          <mat-option value="gpt-4">GPT-4</mat-option>
                          <mat-option value="gpt-3.5-turbo">GPT-3.5 Turbo</mat-option>
                        </mat-select>
                      </mat-form-field>
                    </div>
                    <div class="provider-actions">
                      <button mat-button (click)="testProvider(provider)">
                        Test Connection
                      </button>
                      <button mat-button color="warn" (click)="removeProvider(provider)">
                        Remove
                      </button>
                    </div>
                  </div>
                </div>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>
        
        <mat-tab label="Security">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Security Settings</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <form [formGroup]="securityForm">
                  <mat-form-field appearance="outline">
                    <mat-label>Sandbox Level</mat-label>
                    <mat-select formControlName="sandboxLevel">
                      <mat-option value="none">None</mat-option>
                      <mat-option value="container">Container</mat-option>
                      <mat-option value="vm">Virtual Machine</mat-option>
                    </mat-select>
                  </mat-form-field>
                  
                  <mat-slide-toggle formControlName="requireAuth">
                    Require authentication
                  </mat-slide-toggle>
                  
                  <mat-slide-toggle formControlName="enableLogging">
                    Enable audit logging
                  </mat-slide-toggle>
                  
                  <mat-slide-toggle formControlName="encryptData">
                    Encrypt sensitive data
                  </mat-slide-toggle>
                </form>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" (click)="saveSecurity()">
                  Save Security Settings
                </button>
              </mat-card-actions>
            </mat-card>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .settings-container {
      padding: 20px;
    }
    .tab-content {
      padding: 20px;
    }
    mat-form-field {
      width: 100%;
      margin-bottom: 15px;
    }
    .provider-list {
      display: flex;
      flex-direction: column;
      gap: 15px;
    }
    .provider-card {
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      padding: 15px;
    }
    .provider-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 15px;
    }
    .provider-actions {
      display: flex;
      gap: 10px;
      margin-top: 10px;
    }
    mat-slide-toggle {
      margin: 10px 0;
    }
  `]
})
export class SettingsComponent implements OnInit {
  generalForm: FormGroup;
  securityForm: FormGroup;
  providers: Provider[] = [];
  showKey = false;
  
  constructor(
    private fb: FormBuilder,
    private settingsService: SettingsService,
    private providerService: ProviderService,
    private snackBar: MatSnackBar
  ) {
    this.generalForm = this.fb.group({
      apiUrl: [''],
      theme: ['light'],
      language: ['en'],
      notifications: [true]
    });
    
    this.securityForm = this.fb.group({
      sandboxLevel: ['container'],
      requireAuth: [true],
      enableLogging: [true],
      encryptData: [false]
    });
  }
  
  ngOnInit() {
    this.loadSettings();
    this.loadProviders();
  }
  
  loadSettings() {
    this.settingsService.getSettings().subscribe(settings => {
      this.generalForm.patchValue(settings.general);
      this.securityForm.patchValue(settings.security);
    });
  }
  
  loadProviders() {
    this.providerService.getProviders().subscribe(providers => {
      this.providers = providers;
    });
  }
  
  saveGeneral() {
    this.settingsService.updateGeneralSettings(this.generalForm.value)
      .subscribe(() => {
        this.snackBar.open('Settings saved', 'Close', { duration: 3000 });
      });
  }
  
  saveSecurity() {
    this.settingsService.updateSecuritySettings(this.securityForm.value)
      .subscribe(() => {
        this.snackBar.open('Security settings saved', 'Close', { duration: 3000 });
      });
  }
  
  addProvider() {
    // Open provider dialog
  }
  
  testProvider(provider: Provider) {
    this.providerService.testConnection(provider.id).subscribe(result => {
      const message = result.success ? 'Connection successful' : 'Connection failed';
      this.snackBar.open(message, 'Close', { duration: 3000 });
    });
  }
  
  removeProvider(provider: Provider) {
    // Confirm and remove
  }
}
```

## Remote Module Configuration

### Module Federation Config for Each Remote
```typescript
// apps/[remote-name]/module-federation.config.ts
import { ModuleFederationConfig } from '@nx/webpack';

const config: ModuleFederationConfig = {
  name: '[remote-name]',
  exposes: {
    './Routes': 'apps/[remote-name]/src/app/remote-entry/entry.routes.ts',
  },
  shared: (libraryName, defaultConfig) => {
    if (libraryName.startsWith('@angular/') || 
        libraryName.startsWith('@code-agent/')) {
      return {
        ...defaultConfig,
        singleton: true,
        strictVersion: true,
        requiredVersion: 'auto'
      };
    }
    return defaultConfig;
  }
};

export default config;
```

## Success Criteria
- [ ] All four remote applications generated
- [ ] Dashboard displays real-time metrics
- [ ] Projects management interface functional
- [ ] Chat interface with agent selection
- [ ] Settings with provider configuration
- [ ] Module Federation loading correctly
- [ ] WebSocket communication working
- [ ] All components under 100 lines
- [ ] Material components properly themed

## Next Steps
After completing this phase:
1. Proceed to Phase 6d for i18n and theming
2. Test remote module loading in shell
3. Verify WebSocket updates in dashboard
4. Ensure navigation between remotes works