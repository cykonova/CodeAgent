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
import { 
  ProjectService, 
  Project, 
  ProjectType, 
  ProjectStatus,
  CreateProjectRequest 
} from '@src/data-access';

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
    MatSnackBarModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected title = 'Projects';
  projects: Project[] = [];
  displayedColumns = ['name', 'type', 'status', 'agents', 'updated', 'actions'];
  isLoading = true;
  showCreateForm = false;
  createForm: FormGroup;
  
  projectTypes = Object.values(ProjectType);
  projectStatuses = Object.values(ProjectStatus);

  constructor(
    private projectService: ProjectService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {
    this.createForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      path: ['', Validators.required],
      type: [ProjectType.WebApplication, Validators.required],
      language: [''],
      framework: [''],
      buildCommand: [''],
      testCommand: [''],
      startCommand: ['']
    });
  }

  ngOnInit(): void {
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

  createProject(): void {
    if (this.createForm.valid) {
      const formValue = this.createForm.value;
      const request: CreateProjectRequest = {
        name: formValue.name,
        description: formValue.description,
        path: formValue.path,
        type: formValue.type,
        configuration: {
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
          this.showCreateForm = false;
          this.createForm.reset();
          this.loadProjects();
        },
        error: (error) => {
          console.error('Failed to create project:', error);
          this.showError('Failed to create project');
        }
      });
    }
  }

  openProject(project: Project): void {
    this.projectService.setCurrentProject(project);
    this.showSuccess(`Opened project "${project.name}"`);
  }

  editProject(project: Project): void {
    // TODO: Implement edit dialog
    console.log('Edit project:', project);
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

  archiveProject(project: Project): void {
    this.projectService.archiveProject(project.id).subscribe({
      next: () => {
        this.showSuccess(`Project "${project.name}" archived`);
        this.loadProjects();
      },
      error: (error) => {
        console.error('Failed to archive project:', error);
        this.showError('Failed to archive project');
      }
    });
  }

  getStatusColor(status: ProjectStatus): string {
    switch (status) {
      case ProjectStatus.Active:
        return 'primary';
      case ProjectStatus.Inactive:
        return 'accent';
      case ProjectStatus.Archived:
        return 'warn';
      case ProjectStatus.Error:
        return 'warn';
      default:
        return '';
    }
  }

  getTypeIcon(type: ProjectType): string {
    switch (type) {
      case ProjectType.WebApplication:
        return 'web';
      case ProjectType.API:
        return 'api';
      case ProjectType.Library:
        return 'library_books';
      case ProjectType.Microservice:
        return 'hub';
      case ProjectType.Mobile:
        return 'phone_android';
      case ProjectType.Desktop:
        return 'desktop_windows';
      case ProjectType.CLI:
        return 'terminal';
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