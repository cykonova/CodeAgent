import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { 
  Project, 
  CreateProjectRequest, 
  UpdateProjectRequest,
  WorkflowTemplate 
} from '../models/project.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private projectsSubject = new BehaviorSubject<Project[]>([]);
  private currentProjectSubject = new BehaviorSubject<Project | null>(null);
  
  public projects$ = this.projectsSubject.asObservable();
  public currentProject$ = this.currentProjectSubject.asObservable();

  constructor(private api: ApiService) {
    this.loadProjects();
  }

  loadProjects(): void {
    this.api.get<Project[]>('/projects').subscribe(
      projects => this.projectsSubject.next(projects)
    );
  }

  getProjects(): Observable<Project[]> {
    return this.api.get<Project[]>('/projects')
      .pipe(tap(projects => this.projectsSubject.next(projects)));
  }

  getProject(id: string): Observable<Project> {
    return this.api.get<Project>(`/projects/${id}`)
      .pipe(tap(project => this.currentProjectSubject.next(project)));
  }

  createProject(request: CreateProjectRequest): Observable<Project> {
    return this.api.post<Project>('/projects', request)
      .pipe(tap(() => this.loadProjects()));
  }

  updateProject(id: string, request: UpdateProjectRequest): Observable<Project> {
    return this.api.put<Project>(`/projects/${id}`, request)
      .pipe(tap(() => this.loadProjects()));
  }

  deleteProject(id: string): Observable<void> {
    return this.api.delete<void>(`/projects/${id}`)
      .pipe(tap(() => {
        this.loadProjects();
        if (this.currentProjectSubject.value?.id === id) {
          this.currentProjectSubject.next(null);
        }
      }));
  }

  setCurrentProject(project: Project | null): void {
    this.currentProjectSubject.next(project);
  }

  getWorkflows(projectId: string): Observable<WorkflowTemplate[]> {
    return this.api.get<WorkflowTemplate[]>(`/projects/${projectId}/workflows`);
  }

  createWorkflow(projectId: string, workflow: WorkflowTemplate): Observable<WorkflowTemplate> {
    return this.api.post<WorkflowTemplate>(`/projects/${projectId}/workflows`, workflow);
  }

  executeWorkflow(projectId: string, workflowId: string): Observable<any> {
    return this.api.post(`/projects/${projectId}/workflows/${workflowId}/execute`, {});
  }

  archiveProject(id: string): Observable<Project> {
    return this.updateProject(id, { status: 'archived' as any });
  }

  activateProject(id: string): Observable<Project> {
    return this.updateProject(id, { status: 'active' as any });
  }
}