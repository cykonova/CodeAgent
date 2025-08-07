import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, retry, tap } from 'rxjs/operators';

export interface GitStatus {
  branch: string;
  ahead: number;
  behind: number;
  staged: string[];
  modified: string[];
  untracked: string[];
  deleted: string[];
}

export interface GitCommit {
  sha: string;
  message: string;
  author: string;
  date: Date;
  files: string[];
}

export interface GitDiff {
  path: string;
  status: 'added' | 'modified' | 'deleted';
  additions: number;
  deletions: number;
  patch: string;
}

@Injectable({
  providedIn: 'root'
})
export class GitService {
  private apiUrl = 'http://localhost:5001/api/git';
  private loadingState = signal<boolean>(false);
  private errorState = signal<string | null>(null);
  private currentStatus = signal<GitStatus | null>(null);
  
  // Observable for real-time git updates
  private gitChanges$ = new BehaviorSubject<{ action: string; data?: any } | null>(null);
  
  constructor(private http: HttpClient) {}

  getLoadingState() {
    return this.loadingState;
  }

  getErrorState() {
    return this.errorState;
  }

  getCurrentStatus() {
    return this.currentStatus;
  }

  getGitChanges() {
    return this.gitChanges$.asObservable();
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    let errorMessage = 'Git operation failed';
    
    if (error.error instanceof ErrorEvent) {
      errorMessage = error.error.message;
    } else {
      errorMessage = error.error?.error || error.message || `Git error: ${error.status}`;
    }
    
    console.error('GitService error:', errorMessage, error);
    this.errorState.set(errorMessage);
    
    return throwError(() => new Error(errorMessage));
  }

  private clearError(): void {
    this.errorState.set(null);
  }
  
  // Get git status
  getStatus(path?: string): Observable<GitStatus> {
    this.clearError();
    this.loadingState.set(true);
    
    const params = path ? new HttpParams().set('path', path) : undefined;
    return this.http.get<GitStatus>(`${this.apiUrl}/status`, { params }).pipe(
      retry(1),
      tap((status) => {
        this.currentStatus.set(status);
        this.loadingState.set(false);
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Stage files
  stageFiles(files: string[]): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post<any>(`${this.apiUrl}/stage`, { files }, { headers }).pipe(
      tap(() => {
        this.loadingState.set(false);
        this.gitChanges$.next({ action: 'stage', data: files });
        this.refreshStatus();
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Unstage files
  unstageFiles(files: string[]): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post<any>(`${this.apiUrl}/unstage`, { files }, { headers }).pipe(
      tap(() => {
        this.loadingState.set(false);
        this.gitChanges$.next({ action: 'unstage', data: files });
        this.refreshStatus();
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Commit changes
  commit(message: string, files?: string[]): Observable<GitCommit> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post<GitCommit>(`${this.apiUrl}/commit`, { message, files }, { headers }).pipe(
      tap(() => {
        this.loadingState.set(false);
        this.gitChanges$.next({ action: 'commit', data: message });
        this.refreshStatus();
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Get commit history
  getHistory(limit: number = 50): Observable<GitCommit[]> {
    this.clearError();
    this.loadingState.set(true);
    
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<GitCommit[]>(`${this.apiUrl}/history`, { params }).pipe(
      retry(1),
      tap(() => this.loadingState.set(false)),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Get diff for files
  getDiff(files?: string[]): Observable<GitDiff[]> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    
    const body = files ? { files } : {};
    return this.http.post<GitDiff[]>(`${this.apiUrl}/diff`, body, { headers }).pipe(
      retry(1),
      tap(() => this.loadingState.set(false)),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Push changes
  push(branch?: string): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post(`${this.apiUrl}/push`, { branch }, { headers }).pipe(
      tap(() => {
        this.loadingState.set(false);
        this.gitChanges$.next({ action: 'push', data: branch });
        this.refreshStatus();
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Pull changes
  pull(branch?: string): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post(`${this.apiUrl}/pull`, { branch }, { headers }).pipe(
      tap(() => {
        this.loadingState.set(false);
        this.gitChanges$.next({ action: 'pull', data: branch });
        this.refreshStatus();
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Create branch
  createBranch(name: string): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post(`${this.apiUrl}/branch`, { name }, { headers }).pipe(
      tap(() => {
        this.loadingState.set(false);
        this.gitChanges$.next({ action: 'branch_create', data: name });
        this.refreshStatus();
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Switch branch
  switchBranch(name: string): Observable<any> {
    this.clearError();
    this.loadingState.set(true);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post(`${this.apiUrl}/checkout`, { branch: name }, { headers }).pipe(
      tap(() => {
        this.loadingState.set(false);
        this.gitChanges$.next({ action: 'branch_switch', data: name });
        this.refreshStatus();
      }),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }
  
  // Get branches
  getBranches(): Observable<string[]> {
    this.clearError();
    this.loadingState.set(true);
    
    return this.http.get<string[]>(`${this.apiUrl}/branches`).pipe(
      retry(1),
      tap(() => this.loadingState.set(false)),
      catchError((error) => {
        this.loadingState.set(false);
        return this.handleError(error);
      })
    );
  }

  // Refresh status (private helper)
  private refreshStatus(): void {
    this.getStatus().subscribe({
      error: (error) => {
        console.warn('Failed to refresh git status:', error);
      }
    });
  }

  // Utility methods
  formatCommitDate(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMinutes = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMinutes < 60) {
      return `${diffMinutes} minutes ago`;
    } else if (diffHours < 24) {
      return `${diffHours} hours ago`;
    } else if (diffDays < 30) {
      return `${diffDays} days ago`;
    } else {
      return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      }).format(date);
    }
  }

  shortenHash(hash: string): string {
    return hash.substring(0, 7);
  }
}