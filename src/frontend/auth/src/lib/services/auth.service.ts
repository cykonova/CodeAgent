import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

export interface User {
  id: string;
  email: string;
  name: string;
  role: string;
  avatar?: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  user: User;
  expiresIn: number;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  organization?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  
  private readonly TOKEN_KEY = 'code_agent_token';
  private readonly REFRESH_TOKEN_KEY = 'code_agent_refresh_token';
  private readonly USER_KEY = 'code_agent_user';
  
  private currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  
  private apiUrl = '/api/auth'; // Will be updated with environment config
  
  constructor() {
    this.checkTokenExpiration();
  }
  
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => this.handleAuthResponse(response)),
      catchError(error => this.handleError(error))
    );
  }
  
  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request).pipe(
      tap(response => this.handleAuthResponse(response)),
      catchError(error => this.handleError(error))
    );
  }
  
  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    sessionStorage.clear();
    
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    
    this.router.navigate(['/login']);
  }
  
  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }
    
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(response => this.handleAuthResponse(response)),
      catchError(error => {
        this.logout();
        return throwError(() => error);
      })
    );
  }
  
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }
  
  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }
  
  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }
  
  updateProfile(user: Partial<User>): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/profile`, user).pipe(
      tap(updatedUser => {
        const currentUser = this.currentUserSubject.value;
        if (currentUser) {
          const updated = { ...currentUser, ...updatedUser };
          localStorage.setItem(this.USER_KEY, JSON.stringify(updated));
          this.currentUserSubject.next(updated);
        }
      }),
      catchError(error => this.handleError(error))
    );
  }
  
  changePassword(oldPassword: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/change-password`, { oldPassword, newPassword }).pipe(
      catchError(error => this.handleError(error))
    );
  }
  
  requestPasswordReset(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-password`, { email }).pipe(
      catchError(error => this.handleError(error))
    );
  }
  
  resetPassword(token: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-password/confirm`, { token, newPassword }).pipe(
      catchError(error => this.handleError(error))
    );
  }
  
  private handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
    
    this.currentUserSubject.next(response.user);
    this.isAuthenticatedSubject.next(true);
    
    // Set token expiration check
    if (response.expiresIn) {
      setTimeout(() => this.checkTokenExpiration(), (response.expiresIn - 60) * 1000);
    }
  }
  
  private handleError(error: any): Observable<never> {
    console.error('Authentication error:', error);
    return throwError(() => error);
  }
  
  private getUserFromStorage(): User | null {
    const userStr = localStorage.getItem(this.USER_KEY);
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
  }
  
  private hasValidToken(): boolean {
    const token = this.getToken();
    if (!token) return false;
    
    try {
      const payload = this.decodeToken(token);
      const exp = payload.exp * 1000;
      return Date.now() < exp;
    } catch {
      return false;
    }
  }
  
  private decodeToken(token: string): any {
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new Error('Invalid token');
    }
    
    const decoded = atob(parts[1]);
    return JSON.parse(decoded);
  }
  
  private checkTokenExpiration(): void {
    if (!this.hasValidToken()) {
      const refreshToken = this.getRefreshToken();
      if (refreshToken) {
        this.refreshToken().subscribe({
          error: () => this.logout()
        });
      } else {
        this.logout();
      }
    }
  }
}