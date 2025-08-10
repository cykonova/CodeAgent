import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { 
  Agent, 
  CreateAgentRequest, 
  UpdateAgentRequest,
  AgentExecution,
  ExecutionStatus 
} from '../models/agent.model';

@Injectable({
  providedIn: 'root'
})
export class AgentService {
  private agentsSubject = new BehaviorSubject<Agent[]>([]);
  private executionsSubject = new BehaviorSubject<AgentExecution[]>([]);
  private activeExecutionSubject = new BehaviorSubject<AgentExecution | null>(null);
  
  public agents$ = this.agentsSubject.asObservable();
  public executions$ = this.executionsSubject.asObservable();
  public activeExecution$ = this.activeExecutionSubject.asObservable();

  constructor(private api: ApiService) {
    this.loadAgents();
  }

  loadAgents(): void {
    this.api.get<Agent[]>('/agents').subscribe(
      agents => this.agentsSubject.next(agents)
    );
  }

  getAgents(): Observable<Agent[]> {
    return this.api.get<Agent[]>('/agents')
      .pipe(tap(agents => this.agentsSubject.next(agents)));
  }

  getAgent(id: string): Observable<Agent> {
    return this.api.get<Agent>(`/agents/${id}`);
  }

  createAgent(request: CreateAgentRequest): Observable<Agent> {
    return this.api.post<Agent>('/agents', request)
      .pipe(tap(() => this.loadAgents()));
  }

  updateAgent(id: string, request: UpdateAgentRequest): Observable<Agent> {
    return this.api.put<Agent>(`/agents/${id}`, request)
      .pipe(tap(() => this.loadAgents()));
  }

  deleteAgent(id: string): Observable<void> {
    return this.api.delete<void>(`/agents/${id}`)
      .pipe(tap(() => this.loadAgents()));
  }

  executeAgent(agentId: string, input: string, projectId?: string): Observable<AgentExecution> {
    const request = { input, projectId };
    return this.api.post<AgentExecution>(`/agents/${agentId}/execute`, request)
      .pipe(tap(execution => {
        this.activeExecutionSubject.next(execution);
        this.loadExecutions(agentId);
      }));
  }

  stopExecution(executionId: string): Observable<void> {
    return this.api.post<void>(`/executions/${executionId}/stop`, {})
      .pipe(tap(() => {
        if (this.activeExecutionSubject.value?.id === executionId) {
          this.activeExecutionSubject.next(null);
        }
      }));
  }

  getExecutions(agentId: string): Observable<AgentExecution[]> {
    return this.api.get<AgentExecution[]>(`/agents/${agentId}/executions`)
      .pipe(tap(executions => this.executionsSubject.next(executions)));
  }

  getExecution(executionId: string): Observable<AgentExecution> {
    return this.api.get<AgentExecution>(`/executions/${executionId}`);
  }

  private loadExecutions(agentId: string): void {
    this.getExecutions(agentId).subscribe();
  }

  getAgentMetrics(agentId: string): Observable<any> {
    return this.api.get(`/agents/${agentId}/metrics`);
  }

  getAgentTypes(): string[] {
    return [
      'code-generator',
      'code-reviewer',
      'test-generator',
      'documentation',
      'debugger',
      'refactoring',
      'security',
      'performance',
      'custom'
    ];
  }
}