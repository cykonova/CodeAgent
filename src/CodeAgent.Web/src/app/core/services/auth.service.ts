import { Injectable, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BehaviorSubject, Observable, Subject, throwError, of } from 'rxjs';
import { tap, catchError, map, takeUntil, take } from 'rxjs/operators';
import { WebSocketService } from './websocket.service';
import { 
  User, 
  AuthTokens, 
  LoginCredentials, 
  RegistrationData, 
  AuthResponse, 
  AuthState 
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService implements OnDestroy {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly USER_KEY = 'auth_user';
  
  private authStateSubject = new BehaviorSubject<AuthState>({
    isAuthenticated: false,
    user: null,
    tokens: null,
    loading: false,
    error: null
  });
  
  private tokenRefreshTimer?: any;
  private destroy$ = new Subject<void>();
  
  public authState$ = this.authStateSubject.asObservable();
  public isAuthenticated$ = this.authState$.pipe(map(state => state.isAuthenticated));
  public currentUser$ = this.authState$.pipe(map(state => state.user));
  public loading$ = this.authState$.pipe(map(state => state.loading));
  
  constructor(
    private wsService: WebSocketService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.initializeAuth();
  }
  
  ngOnDestroy(): void {
    this.clearTokenRefreshTimer();
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeAuth(): void {
    const token = this.getStoredToken();
    const user = this.getStoredUser();
    
    if (token && user) {
      if (this.isTokenValid(token)) {
        this.setAuthState({
          isAuthenticated: true,
          user,
          tokens: { accessToken: token } as AuthTokens,
          loading: false,
          error: null
        });
        
        this.scheduleTokenRefresh();
        this.connectWebSocket();
      } else {
        this.refreshToken().subscribe();
      }
    }
  }

  private isTokenValid(token: string): boolean {
    try {
      const payload = this.decodeToken(token);
      const expirationTime = payload.exp * 1000;
      return Date.now() < expirationTime - 60000;
    } catch {
      return false;
    }
  }

  private decodeToken(token: string): any {
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new Error('Invalid token format');
    }
    
    const payload = parts[1];
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(decoded);
  }

  public login(credentials: LoginCredentials): Observable<AuthResponse> {
    this.updateAuthState({ loading: true, error: null });
    
    return this.wsService.request<LoginCredentials, any>('auth', credentials).pipe(
      map(response => response as AuthResponse),
      tap(response => {
        this.storeAuthData(response, credentials.rememberMe);
        
        this.setAuthState({
          isAuthenticated: true,
          user: response.user,
          tokens: response.tokens,
          loading: false,
          error: null
        });
        
        this.scheduleTokenRefresh();
        this.connectWebSocket();
        
        this.router.navigate(['/dashboard']);
      }),
      catchError(error => {
        this.updateAuthState({
          loading: false,
          error: error.message || 'Login failed'
        });
        return throwError(() => error);
      })
    );
  }

  public register(data: RegistrationData): Observable<AuthResponse> {
    this.updateAuthState({ loading: true, error: null });
    
    return this.wsService.request<RegistrationData, any>('register', data).pipe(
      map(response => response as AuthResponse),
      tap(response => {
        this.updateAuthState({
          loading: false,
          error: null
        });
        
        this.router.navigate(['/auth/verify-email'], {
          queryParams: { email: data.email }
        });
      }),
      catchError(error => {
        this.updateAuthState({
          loading: false,
          error: error.message || 'Registration failed'
        });
        return throwError(() => error);
      })
    );
  }

  public logout(): void {
    const token = this.getStoredToken();
    
    if (token) {
      this.wsService.send('logout', {});
    }
    
    this.completeLogout();
  }

  private completeLogout(): void {
    this.clearAuthData();
    
    this.setAuthState({
      isAuthenticated: false,
      user: null,
      tokens: null,
      loading: false,
      error: null
    });
    
    this.clearTokenRefreshTimer();
    this.wsService.disconnect();
    this.router.navigate(['/login']);
  }

  private storeAuthData(response: AuthResponse, persistent: boolean = false): void {
    const storage = persistent ? localStorage : sessionStorage;
    
    storage.setItem(this.TOKEN_KEY, response.tokens.accessToken);
    
    if (response.tokens.refreshToken) {
      storage.setItem(this.REFRESH_TOKEN_KEY, response.tokens.refreshToken);
    }
    
    storage.setItem(this.USER_KEY, JSON.stringify(response.user));
  }

  private clearAuthData(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    sessionStorage.removeItem(this.TOKEN_KEY);
    sessionStorage.removeItem(this.REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(this.USER_KEY);
  }

  private getStoredToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY) || 
           sessionStorage.getItem(this.TOKEN_KEY);
  }

  private getStoredRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY) || 
           sessionStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  private getStoredUser(): User | null {
    const userJson = localStorage.getItem(this.USER_KEY) || 
                     sessionStorage.getItem(this.USER_KEY);
    
    if (userJson) {
      try {
        return JSON.parse(userJson);
      } catch {
        return null;
      }
    }
    
    return null;
  }

  public getAccessToken(): string | null {
    return this.authStateSubject.value.tokens?.accessToken || null;
  }

  private refreshToken(): Observable<AuthTokens> {
    const refreshToken = this.getStoredRefreshToken();
    
    if (!refreshToken) {
      this.completeLogout();
      return throwError(() => new Error('No refresh token available'));
    }
    
    return this.wsService.request<any, any>('refresh', { 
      refreshToken 
    }).pipe(
      map(response => response as AuthTokens),
      tap(tokens => {
        const storage = localStorage.getItem(this.TOKEN_KEY) ? 
                       localStorage : sessionStorage;
        storage.setItem(this.TOKEN_KEY, tokens.accessToken);
        
        if (tokens.refreshToken) {
          storage.setItem(this.REFRESH_TOKEN_KEY, tokens.refreshToken);
        }
        
        this.updateAuthState({ tokens });
        this.scheduleTokenRefresh();
      }),
      catchError(error => {
        this.completeLogout();
        return throwError(() => error);
      })
    );
  }

  private scheduleTokenRefresh(): void {
    this.clearTokenRefreshTimer();
    
    const token = this.getStoredToken();
    if (!token) return;
    
    try {
      const payload = this.decodeToken(token);
      const expirationTime = payload.exp * 1000;
      const refreshTime = expirationTime - Date.now() - 60000;
      
      if (refreshTime > 0) {
        this.tokenRefreshTimer = setTimeout(() => {
          this.refreshToken().subscribe();
        }, refreshTime);
      }
    } catch (error) {
      console.error('Failed to schedule token refresh:', error);
    }
  }

  private clearTokenRefreshTimer(): void {
    if (this.tokenRefreshTimer) {
      clearTimeout(this.tokenRefreshTimer);
      this.tokenRefreshTimer = undefined;
    }
  }

  public updateProfile(updates: Partial<User>): Observable<User> {
    return this.wsService.request<Partial<User>, any>('updateProfile', updates).pipe(
      map(response => response as User),
      tap(user => {
        const storage = localStorage.getItem(this.USER_KEY) ? 
                       localStorage : sessionStorage;
        storage.setItem(this.USER_KEY, JSON.stringify(user));
        
        this.updateAuthState({ user });
      })
    );
  }

  public changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.wsService.request('changePassword', {
      currentPassword,
      newPassword
    }).pipe(map(() => void 0));
  }

  public resetPassword(email: string): Observable<void> {
    return this.wsService.request('resetPassword', { email }).pipe(map(() => void 0));
  }

  public confirmResetPassword(token: string, newPassword: string): Observable<void> {
    return this.wsService.request('confirmReset', { 
      token, 
      newPassword 
    }).pipe(map(() => void 0));
  }

  public verifyEmail(token: string): Observable<void> {
    return this.wsService.request('verifyEmail', { token }).pipe(
      map(() => void 0),
      tap(() => {
        const user = this.authStateSubject.value.user;
        if (user) {
          user.emailVerified = true;
          this.updateAuthState({ user });
        }
      })
    );
  }

  public resendVerificationEmail(): Observable<void> {
    return this.wsService.request('resendVerification', {}).pipe(map(() => void 0));
  }

  public hasRole(role: string): boolean {
    const user = this.authStateSubject.value.user;
    return user?.roles?.includes(role) || false;
  }

  public hasAnyRole(roles: string[]): boolean {
    const user = this.authStateSubject.value.user;
    return roles.some(role => user?.roles?.includes(role)) || false;
  }

  public hasPermission(permission: string): boolean {
    const user = this.authStateSubject.value.user;
    return user?.permissions?.includes(permission) || false;
  }

  public hasAnyPermission(permissions: string[]): boolean {
    const user = this.authStateSubject.value.user;
    return permissions.some(perm => user?.permissions?.includes(perm)) || false;
  }

  public canAccess(resource: string): Observable<boolean> {
    return this.wsService.request<{ resource: string }, any>('canAccess', { resource }).pipe(
      map(response => response.result as boolean)
    );
  }

  private connectWebSocket(): void {
    const token = this.getStoredToken();
    if (token) {
      this.wsService.send('auth', { token });
      
      this.wsService.on('auth_response')
        .pipe(takeUntil(this.destroy$))
        .subscribe(response => {
          if (!(response as any).success) {
            this.completeLogout();
          }
        });
    }
  }

  private setAuthState(state: AuthState): void {
    this.authStateSubject.next(state);
  }

  private updateAuthState(updates: Partial<AuthState>): void {
    const currentState = this.authStateSubject.value;
    this.authStateSubject.next({
      ...currentState,
      ...updates
    });
  }

  public getAuthState(): AuthState {
    return this.authStateSubject.value;
  }
}