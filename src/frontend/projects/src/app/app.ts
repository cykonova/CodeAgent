import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatPaginatorModule } from '@angular/material/paginator';
import { 
  ProjectService, 
  Project, 
  ProjectType, 
  ProjectStatus,
  CreateProjectRequest,
  HeaderService 
} from '@src/data-access';
import { SkeletonLoaderComponent } from '@src/ui-components';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatSidenavModule,
    MatPaginatorModule,
    SkeletonLoaderComponent
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected title = 'Projects';
  projects: Project[] = [];
  displayedColumns = ['name', 'type', 'status', 'lastRun', 'updated', 'actions'];
  isLoading = true;
  sidenavOpened = false;
  editMode = false;
  selectedProject: Project | null = null;
  projectForm: FormGroup;
  
  projectTypes = Object.values(ProjectType);
  projectStatuses = Object.values(ProjectStatus);

  constructor(
    private projectService: ProjectService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private headerService: HeaderService
  ) {
    this.projectForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      path: ['', Validators.required],
      type: [ProjectType.Standard, Validators.required],
      language: [''],
      framework: [''],
      buildCommand: [''],
      testCommand: [''],
      startCommand: ['']
    });
  }

  ngOnInit(): void {
    this.headerService.setPageTitle('Projects');
    this.loadProjects();
  }

  loadProjects(): void {
    this.isLoading = true;
    this.projectService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load projects:', error);
        this.showError('Failed to load projects');
        this.isLoading = false;
      }
    });
  }

  saveProject(): void {
    if (this.projectForm.valid) {
      const formValue = this.projectForm.value;
      
      if (this.editMode && this.selectedProject) {
        // Update existing project
        const updateRequest = {
          name: formValue.name,
          description: formValue.description,
          configuration: {
            language: formValue.language,
            framework: formValue.framework,
            buildCommand: formValue.buildCommand,
            testCommand: formValue.testCommand,
            startCommand: formValue.startCommand
          }
        };
        
        this.projectService.updateProject(this.selectedProject.id, updateRequest).subscribe({
          next: (project) => {
            this.showSuccess(`Project "${project.name}" updated successfully`);
            this.closeSidenav();
            this.loadProjects();
          },
          error: (error) => {
            console.error('Failed to update project:', error);
            this.showError('Failed to update project');
          }
        });
      } else {
        // Create new project
        const request: CreateProjectRequest = {
          name: formValue.name,
          description: formValue.description,
          type: formValue.type,
          configuration: {
            workingDirectory: formValue.path,
            language: formValue.language,
            framework: formValue.framework,
            buildCommand: formValue.buildCommand,
            testCommand: formValue.testCommand,
            startCommand: formValue.startCommand
          }
        };

        this.projectService.createProject(request).subscribe({
          next: (project) => {
            this.showSuccess(`Project "${project.name}" created successfully`);
            this.closeSidenav();
            this.loadProjects();
          },
          error: (error) => {
            console.error('Failed to create project:', error);
            this.showError('Failed to create project');
          }
        });
      }
    }
  }

  openProject(project: Project): void {
    this.projectService.setCurrentProject(project);
    this.showSuccess(`Opened project "${project.name}"`);
  }

  openCreateProject(): void {
    this.editMode = false;
    this.selectedProject = null;
    this.projectForm.reset({
      type: ProjectType.Standard
    });
    this.sidenavOpened = true;
  }

  editProject(project: Project): void {
    this.editMode = true;
    this.selectedProject = project;
    this.projectForm.patchValue({
      name: project.name,
      description: project.description,
      type: project.type,
      language: project.configuration?.language,
      framework: project.configuration?.framework,
      buildCommand: project.configuration?.buildCommand,
      testCommand: project.configuration?.testCommand,
      startCommand: project.configuration?.startCommand
    });
    this.sidenavOpened = true;
  }

  closeSidenav(): void {
    this.sidenavOpened = false;
    this.selectedProject = null;
    this.projectForm.reset();
  }

  deleteProject(project: Project): void {
    if (confirm(`Are you sure you want to delete project "${project.name}"?`)) {
      this.projectService.deleteProject(project.id).subscribe({
        next: () => {
          this.showSuccess(`Project "${project.name}" deleted`);
          this.loadProjects();
        },
        error: (error) => {
          console.error('Failed to delete project:', error);
          this.showError('Failed to delete project');
        }
      });
    }
  }

  pauseProject(project: Project): void {
    // TODO: Implement pause functionality
    this.showSuccess(`Project "${project.name}" paused`);
  }

  getStatusColor(status: ProjectStatus): string {
    switch (status) {
      case ProjectStatus.Running:
        return 'primary';
      case ProjectStatus.Idle:
        return 'accent';
      case ProjectStatus.Paused:
        return 'accent';
      case ProjectStatus.Completed:
        return 'primary';
      case ProjectStatus.Failed:
        return 'warn';
      case ProjectStatus.Cancelled:
        return 'warn';
      default:
        return '';
    }
  }

  getTypeIcon(type: ProjectType): string {
    switch (type) {
      case ProjectType.Standard:
        return 'folder';
      case ProjectType.Fast:
        return 'speed';
      case ProjectType.Quality:
        return 'verified';
      case ProjectType.Budget:
        return 'savings';
      case ProjectType.Custom:
        return 'tune';
      default:
        return 'folder';
    }
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: 'success-snackbar'
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: 'error-snackbar'
    });
  }
}