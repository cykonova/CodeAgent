import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatGridListModule } from '@angular/material/grid-list';
import { StatCardComponent, SkeletonLoaderComponent } from '@code-agent/ui/components';
import { AgentService, ProjectService, ProviderService } from '@code-agent/data-access';
import { WebSocketService } from '@code-agent/util/websocket';

@Component({
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatGridListModule,
    StatCardComponent,
    SkeletonLoaderComponent
  ],
  selector: 'app-dashboard',
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected title = 'Dashboard';
  
  // Metrics
  activeAgents = 0;
  runningProjects = 0;
  completedTasks = 0;
  totalExecutions = 0;
  
  // Status
  systemStatus: 'online' | 'offline' | 'warning' = 'online';
  isLoading = true;
  
  // Recent activity
  recentActivity: any[] = [];
  
  constructor(
    private agentService: AgentService,
    private projectService: ProjectService,
    private providerService: ProviderService,
    private websocket: WebSocketService
  ) {}
  
  ngOnInit(): void {
    this.loadMetrics();
    this.subscribeToUpdates();
  }
  
  private loadMetrics(): void {
    this.isLoading = true;
    
    // Load agents
    this.agentService.getAgents().subscribe(agents => {
      this.activeAgents = agents.filter(a => a.status !== 'offline').length;
    });
    
    // Load projects
    this.projectService.getProjects().subscribe(projects => {
      this.runningProjects = projects.filter(p => p.state?.status === 'running').length;
    });
    
    // Load providers and check system status
    this.providerService.getProviders().subscribe(providers => {
      const hasActiveProvider = providers.some(p => p.enabled && p.status.isConnected);
      this.systemStatus = hasActiveProvider ? 'online' : 'warning';
      this.isLoading = false;
    });
    
    // Simulate completed tasks (should come from backend)
    this.completedTasks = 42;
    this.totalExecutions = 156;
  }
  
  private subscribeToUpdates(): void {
    this.websocket.connect();
    
    this.websocket.messages$.subscribe(message => {
      if (message.type === 'metrics.update') {
        this.updateMetrics(message.data);
      } else if (message.type === 'activity') {
        this.addActivity(message.data);
      }
    });
  }
  
  private updateMetrics(data: any): void {
    if (data.activeAgents !== undefined) this.activeAgents = data.activeAgents;
    if (data.runningProjects !== undefined) this.runningProjects = data.runningProjects;
    if (data.completedTasks !== undefined) this.completedTasks = data.completedTasks;
    if (data.totalExecutions !== undefined) this.totalExecutions = data.totalExecutions;
  }
  
  private addActivity(activity: any): void {
    this.recentActivity.unshift(activity);
    if (this.recentActivity.length > 10) {
      this.recentActivity.pop();
    }
  }
  
  refreshMetrics(): void {
    this.loadMetrics();
  }
}
