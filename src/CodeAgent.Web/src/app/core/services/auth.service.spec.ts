import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { WebSocketService } from './websocket.service';
import { AuthResponse, User, AuthTokens } from '../models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let wsService: jasmine.SpyObj<WebSocketService>;
  let router: jasmine.SpyObj<Router>;
  let snackBar: jasmine.SpyObj<MatSnackBar>;
  
  beforeEach(() => {
    const wsSpy = jasmine.createSpyObj('WebSocketService', ['send', 'request', 'on', 'disconnect']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    const snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);
    
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: WebSocketService, useValue: wsSpy },
        { provide: Router, useValue: routerSpy },
        { provide: MatSnackBar, useValue: snackBarSpy }
      ]
    });
    
    service = TestBed.inject(AuthService);
    wsService = TestBed.inject(WebSocketService) as jasmine.SpyObj<WebSocketService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    snackBar = TestBed.inject(MatSnackBar) as jasmine.SpyObj<MatSnackBar>;
    
    localStorage.clear();
    sessionStorage.clear();
  });
  
  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('should login successfully', (done) => {
      const mockResponse: AuthResponse = {
        user: { 
          id: '1', 
          email: 'test@example.com',
          firstName: 'Test',
          lastName: 'User',
          displayName: 'Test User',
          roles: ['user'],
          permissions: [],
          createdAt: new Date(),
          lastLogin: new Date(),
          emailVerified: true
        } as User,
        tokens: { 
          accessToken: 'token123',
          expiresIn: 3600,
          tokenType: 'Bearer'
        } as AuthTokens,
        sessionId: 'session123'
      };
      
      wsService.request.and.returnValue(of(mockResponse as any));
      wsService.on.and.returnValue(of({ type: 'auth_response', success: true } as any));
      
      service.login({ 
        email: 'test@example.com', 
        password: 'password123' 
      }).subscribe(() => {
        expect(service.getAuthState().isAuthenticated).toBe(true);
        expect(service.getAuthState().user).toEqual(mockResponse.user);
        expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
        done();
      });
    });
    
    it('should handle login failure', (done) => {
      wsService.request.and.returnValue(
        throwError(() => ({ message: 'Invalid credentials' }))
      );
      
      service.login({ 
        email: 'test@example.com', 
        password: 'wrong' 
      }).subscribe({
        error: (error) => {
          expect(service.getAuthState().isAuthenticated).toBe(false);
          expect(service.getAuthState().error).toBe('Invalid credentials');
          done();
        }
      });
    });
    
    it('should store auth data persistently when rememberMe is true', (done) => {
      const mockResponse: AuthResponse = {
        user: { 
          id: '1', 
          email: 'test@example.com',
          firstName: 'Test',
          lastName: 'User',
          displayName: 'Test User',
          roles: ['user'],
          permissions: [],
          createdAt: new Date(),
          lastLogin: new Date(),
          emailVerified: true
        } as User,
        tokens: { 
          accessToken: 'token123',
          refreshToken: 'refresh123',
          expiresIn: 3600,
          tokenType: 'Bearer'
        } as AuthTokens,
        sessionId: 'session123'
      };
      
      wsService.request.and.returnValue(of(mockResponse as any));
      wsService.on.and.returnValue(of({ type: 'auth_response', success: true } as any));
      
      service.login({ 
        email: 'test@example.com', 
        password: 'password123',
        rememberMe: true
      }).subscribe(() => {
        expect(localStorage.getItem('auth_token')).toBe('token123');
        expect(localStorage.getItem('refresh_token')).toBe('refresh123');
        expect(localStorage.getItem('auth_user')).toBeTruthy();
        done();
      });
    });
  });

  describe('register', () => {
    it('should register successfully', (done) => {
      const mockResponse: AuthResponse = {
        user: { 
          id: '1', 
          email: 'new@example.com',
          firstName: 'New',
          lastName: 'User',
          displayName: 'New User',
          roles: ['user'],
          permissions: [],
          createdAt: new Date(),
          lastLogin: new Date(),
          emailVerified: false
        } as User,
        tokens: { 
          accessToken: 'token123',
          expiresIn: 3600,
          tokenType: 'Bearer'
        } as AuthTokens,
        sessionId: 'session123'
      };
      
      wsService.request.and.returnValue(of(mockResponse as any));
      
      service.register({
        email: 'new@example.com',
        password: 'password123',
        firstName: 'New',
        lastName: 'User',
        acceptTerms: true
      }).subscribe(() => {
        expect(router.navigate).toHaveBeenCalledWith(
          ['/auth/verify-email'],
          { queryParams: { email: 'new@example.com' } }
        );
        done();
      });
    });
    
    it('should handle registration failure', (done) => {
      wsService.request.and.returnValue(
        throwError(() => ({ message: 'Email already exists' }))
      );
      
      service.register({
        email: 'existing@example.com',
        password: 'password123',
        firstName: 'Test',
        lastName: 'User',
        acceptTerms: true
      }).subscribe({
        error: (error) => {
          expect(service.getAuthState().error).toBe('Email already exists');
          done();
        }
      });
    });
  });

  describe('logout', () => {
    it('should logout and clear data', () => {
      localStorage.setItem('auth_token', 'token123');
      localStorage.setItem('auth_user', '{"id":"1"}');
      
      wsService.send.and.returnValue(undefined);
      
      service.logout();
      
      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('auth_user')).toBeNull();
      expect(service.getAuthState().isAuthenticated).toBe(false);
      expect(wsService.disconnect).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('token management', () => {
    it('should get access token', () => {
      const state = service.getAuthState();
      expect(service.getAccessToken()).toBeNull();
      
      service['setAuthState']({
        ...state,
        tokens: { accessToken: 'test-token' } as AuthTokens
      });
      
      expect(service.getAccessToken()).toBe('test-token');
    });
    
    it('should validate token correctly', () => {
      const futureExp = Math.floor(Date.now() / 1000) + 3600;
      const validToken = `header.${btoa(JSON.stringify({ exp: futureExp }))}.signature`;
      
      expect(service['isTokenValid'](validToken)).toBe(true);
      
      const pastExp = Math.floor(Date.now() / 1000) - 3600;
      const expiredToken = `header.${btoa(JSON.stringify({ exp: pastExp }))}.signature`;
      
      expect(service['isTokenValid'](expiredToken)).toBe(false);
    });
  });

  describe('authorization helpers', () => {
    beforeEach(() => {
      const user: User = {
        id: '1',
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        displayName: 'Test User',
        roles: ['user', 'admin'],
        permissions: ['read', 'write'],
        createdAt: new Date(),
        lastLogin: new Date(),
        emailVerified: true
      };
      
      service['setAuthState']({
        isAuthenticated: true,
        user,
        tokens: null,
        loading: false,
        error: null
      });
    });
    
    it('should check if user has role', () => {
      expect(service.hasRole('admin')).toBe(true);
      expect(service.hasRole('super-admin')).toBe(false);
    });
    
    it('should check if user has any role', () => {
      expect(service.hasAnyRole(['super-admin', 'admin'])).toBe(true);
      expect(service.hasAnyRole(['super-admin', 'moderator'])).toBe(false);
    });
    
    it('should check if user has permission', () => {
      expect(service.hasPermission('read')).toBe(true);
      expect(service.hasPermission('delete')).toBe(false);
    });
    
    it('should check if user has any permission', () => {
      expect(service.hasAnyPermission(['delete', 'write'])).toBe(true);
      expect(service.hasAnyPermission(['delete', 'execute'])).toBe(false);
    });
  });

  describe('user operations', () => {
    it('should update profile', (done) => {
      const updatedUser: User = {
        id: '1',
        email: 'test@example.com',
        firstName: 'Updated',
        lastName: 'User',
        displayName: 'Updated User',
        roles: ['user'],
        permissions: [],
        createdAt: new Date(),
        lastLogin: new Date(),
        emailVerified: true
      };
      
      wsService.request.and.returnValue(of(updatedUser as any));
      
      service.updateProfile({ firstName: 'Updated' }).subscribe(user => {
        expect(user).toEqual(updatedUser);
        expect(service.getAuthState().user).toEqual(updatedUser);
        done();
      });
    });
    
    it('should change password', (done) => {
      wsService.request.and.returnValue(of({} as any));
      
      service.changePassword('oldPassword', 'newPassword').subscribe(() => {
        expect(wsService.request).toHaveBeenCalledWith('changePassword', {
          currentPassword: 'oldPassword',
          newPassword: 'newPassword'
        });
        done();
      });
    });
    
    it('should reset password', (done) => {
      wsService.request.and.returnValue(of({} as any));
      
      service.resetPassword('test@example.com').subscribe(() => {
        expect(wsService.request).toHaveBeenCalledWith('resetPassword', {
          email: 'test@example.com'
        });
        done();
      });
    });
    
    it('should verify email', (done) => {
      const user: User = {
        id: '1',
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        displayName: 'Test User',
        roles: ['user'],
        permissions: [],
        createdAt: new Date(),
        lastLogin: new Date(),
        emailVerified: false
      };
      
      service['setAuthState']({
        isAuthenticated: true,
        user,
        tokens: null,
        loading: false,
        error: null
      });
      
      wsService.request.and.returnValue(of({} as any));
      
      service.verifyEmail('verification-token').subscribe(() => {
        expect(service.getAuthState().user?.emailVerified).toBe(true);
        done();
      });
    });
  });
});