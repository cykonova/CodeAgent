import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { map, catchError, retry, tap } from 'rxjs/operators';

export interface FileNode {
  name: string;
  path: string;
  type: 'file' | 'folder';
  children?: FileNode[];
  size?: number;
  modified?: Date;
  extension?: string;
}

export interface FileContent {
  path: string;
  content: string;
  encoding: string;
  size: number;
  modified: Date;
}

export interface FileOperation {
  operation: 'read' | 'write' | 'edit' | 'delete' | 'rename' | 'create';
  path: string;
  content?: string;
  newPath?: string;
  oldContent?: string;
  newContent?: string;
}

export interface PermissionRequest {
  operation: string;
  path: string;
  details?: string;
}

@Injectable({
  providedIn: 'root'
})
export class FileService {
  private apiUrl = 'http://localhost:5001/api/file';
  private currentPath = signal<string>('');
  private loadingState = signal<boolean>(false);
  private errorState = signal<string | null>(null);
  
  // Observable for real-time updates
  private fileChanges$ = new BehaviorSubject<{ action: string; path: string } | null>(null);
  
  constructor(private http: HttpClient) {}

  getCurrentPath() {
    return this.currentPath;
  }

  getLoadingState() {
    return this.loadingState;
  }

  getErrorState() {
    return this.errorState;
  }

  getFileChanges() {
    return this.fileChanges$.asObservable();
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    let errorMessage = 'An error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      errorMessage = error.error?.error || error.message || `Server error: ${error.status}`;
    }
    
    console.error('FileService error:', errorMessage, error);
    this.errorState.set(errorMessage);
    
    return throwError(() => new Error(errorMessage));
  }

  private clearError(): void {
    this.errorState.set(null);
  }
  
  // List files in a directory (connects to actual FileController.BrowseDirectory)
  listFiles(path: string = ''): Observable<FileNode[]> {
    this.clearError();
    this.loadingState.set(true);
    
    const params = new HttpParams().set('path', path);
    return this.http.get<any>(`${this.apiUrl}/browse`, { params }).pipe(
      retry(2),
      map(response => this.convertToFileNodes(response.entries, response.currentPath)),
      tap(() => {
        this.currentPath.set(path);
        this.loadingState.set(false);
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }

  private convertToFileNodes(entries: any[], currentPath: string): FileNode[] {
    return entries.map(entry => ({
      name: entry.name,
      path: entry.path,
      type: entry.isDirectory ? 'folder' : 'file',
      size: entry.size,
      modified: entry.modified ? new Date(entry.modified) : undefined,
      extension: entry.isDirectory ? undefined : entry.name.split('.').pop()?.toLowerCase(),
      children: entry.isDirectory ? [] : undefined
    }));
  }
  
  // Read file content (connects to actual FileController.ReadFile)
  readFile(path: string): Observable<FileContent> {
    this.clearError();
    this.loadingState.set(true);
    
    const params = new HttpParams().set('path', path);
    return this.http.get<any>(`${this.apiUrl}/read`, { params }).pipe(
      retry(1),
      map(response => ({
        path: response.path,
        content: response.content,
        encoding: 'utf-8',
        size: response.content?.length || 0,
        modified: new Date()
      })),
      tap(() => this.loadingState.set(false)),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Edit file (with diff and permission handling)
  editFile(path: string, content: string): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post<any>(`${this.apiUrl}/edit`, { path, content }, { headers })
      .pipe(
        catchError(this.handleError),
        tap((result) => {
          this.loadingState.set(false);
          if (result.applied) {
            this.fileChanges$.next({ action: 'edit', path });
          }
        }),
        catchError((error) => {
          this.loadingState.set(false);
          return throwError(() => error);
        })
      );
  }
  
  // Delete file
  deleteFile(path: string): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const params = new HttpParams().set('path', path);
    return this.http.delete<any>(`${this.apiUrl}`, { params }).pipe(
      catchError(this.handleError),
      tap(() => {
        this.loadingState.set(false);
        this.fileChanges$.next({ action: 'delete', path });
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return throwError(() => error);
      })
    );
  }
  
  // Create new file
  createFile(path: string, content: string = ''): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post<any>(`${this.apiUrl}/create`, { path, content }, { headers })
      .pipe(
        catchError(this.handleError),
        tap(() => {
          this.loadingState.set(false);
          this.fileChanges$.next({ action: 'create', path });
        }),
        catchError((error) => {
          this.loadingState.set(false);
          return throwError(() => error);
        })
      );
  }
  
  // Search files
  searchFiles(pattern: string, directory: string = ''): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const params: any = { pattern };
    if (directory) {
      params.directory = directory;
    }

    return this.http.get<any>(`${this.apiUrl}/search`, { params })
      .pipe(
        retry(1),
        catchError(this.handleError),
        tap(() => this.loadingState.set(false)),
        catchError((error) => {
          this.loadingState.set(false);
          return throwError(() => error);
        })
      );
  }

  // Utility methods
  getFileExtension(filename: string): string {
    return filename.split('.').pop()?.toLowerCase() || '';
  }

  isTextFile(filename: string): boolean {
    const textExtensions = [
      'txt', 'md', 'json', 'js', 'ts', 'html', 'css', 'scss', 'less',
      'py', 'java', 'cs', 'cpp', 'c', 'h', 'php', 'rb', 'go', 'rs',
      'xml', 'yml', 'yaml', 'toml', 'ini', 'cfg', 'conf'
    ];
    return textExtensions.includes(this.getFileExtension(filename));
  }

  isImageFile(filename: string): boolean {
    const imageExtensions = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'svg', 'webp'];
    return imageExtensions.includes(this.getFileExtension(filename));
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }

  // Navigation helpers
  getParentPath(path: string): string {
    return path.split('/').slice(0, -1).join('/') || '/';
  }

  getFileName(path: string): string {
    return path.split('/').pop() || '';
  }

  joinPath(...segments: string[]): string {
    return segments
      .filter(segment => segment && segment !== '/')
      .join('/')
      .replace(/\/+/g, '/');
  }
}