import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';

// Angular Material Modules
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';

import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/services/auth.service';
import { AuthState } from '../../../core/models/auth.models';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let authStateSubject: BehaviorSubject<AuthState>;

  beforeEach(async () => {
    authStateSubject = new BehaviorSubject<AuthState>({
      isAuthenticated: false,
      user: null,
      tokens: null,
      loading: false,
      error: null
    });

    const authServiceSpy = jasmine.createSpyObj('AuthService', ['login', 'getAuthState'], {
      isAuthenticated$: authStateSubject.pipe(map(state => state.isAuthenticated)),
      loading$: authStateSubject.pipe(map(state => state.loading)),
      authState$: authStateSubject.asObservable()
    });

    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatCheckboxModule,
        MatIconModule,
        MatProgressSpinnerModule,
        MatSnackBarModule
      ],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Initialization', () => {
    it('should initialize the form with empty values', () => {
      expect(component.loginForm.get('email')?.value).toBe('');
      expect(component.loginForm.get('password')?.value).toBe('');
      expect(component.loginForm.get('rememberMe')?.value).toBe(false);
    });

    it('should initialize with validators', () => {
      const emailControl = component.loginForm.get('email');
      const passwordControl = component.loginForm.get('password');

      expect(emailControl?.hasError('required')).toBe(true);
      
      emailControl?.setValue('invalid-email');
      expect(emailControl?.hasError('email')).toBe(true);
      
      passwordControl?.setValue('short');
      expect(passwordControl?.hasError('minlength')).toBe(true);
    });
  });

  describe('Remember Me Functionality', () => {
    it('should load remembered email on init', () => {
      localStorage.setItem('remembered_email', 'test@example.com');
      
      component.ngOnInit();
      
      expect(component.loginForm.get('email')?.value).toBe('test@example.com');
      expect(component.loginForm.get('rememberMe')?.value).toBe(true);
      
      localStorage.removeItem('remembered_email');
    });

    it('should save email when remember me is checked', () => {
      component.loginForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: true
      });

      authService.login.and.returnValue(of({
        user: { id: '1', email: 'test@example.com' } as any,
        tokens: { accessToken: 'token' } as any,
        sessionId: 'session'
      }));

      component.onSubmit();

      expect(localStorage.getItem('remembered_email')).toBe('test@example.com');
      localStorage.removeItem('remembered_email');
    });

    it('should remove saved email when remember me is unchecked', () => {
      localStorage.setItem('remembered_email', 'old@example.com');
      
      component.loginForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: false
      });

      authService.login.and.returnValue(of({
        user: { id: '1', email: 'test@example.com' } as any,
        tokens: { accessToken: 'token' } as any,
        sessionId: 'session'
      }));

      component.onSubmit();

      expect(localStorage.getItem('remembered_email')).toBeNull();
    });
  });

  describe('Form Submission', () => {
    it('should not submit if form is invalid', () => {
      component.onSubmit();
      
      expect(authService.login).not.toHaveBeenCalled();
    });

    it('should mark all fields as touched if form is invalid', () => {
      component.onSubmit();
      
      expect(component.loginForm.get('email')?.touched).toBe(true);
      expect(component.loginForm.get('password')?.touched).toBe(true);
    });

    it('should call auth service with correct credentials', () => {
      component.loginForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: false
      });

      authService.login.and.returnValue(of({
        user: { id: '1', email: 'test@example.com' } as any,
        tokens: { accessToken: 'token' } as any,
        sessionId: 'session'
      }));

      component.onSubmit();

      expect(authService.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: false
      });
    });

    it('should handle login error', () => {
      component.loginForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: false
      });

      const error = new Error('Invalid credentials');
      authService.login.and.returnValue(throwError(() => error));
      
      spyOn(console, 'error');
      
      component.onSubmit();

      expect(console.error).toHaveBeenCalledWith('Login failed:', error);
    });
  });

  describe('Password Visibility', () => {
    it('should toggle password visibility', () => {
      expect(component.hidePassword).toBe(true);
      
      component.togglePasswordVisibility();
      expect(component.hidePassword).toBe(false);
      
      component.togglePasswordVisibility();
      expect(component.hidePassword).toBe(true);
    });
  });

  describe('Error Messages', () => {
    it('should return correct email error messages', () => {
      const emailControl = component.loginForm.get('email');
      
      emailControl?.setErrors({ required: true });
      expect(component.getEmailErrorMessage()).toBe('Email is required');
      
      emailControl?.setErrors({ email: true });
      expect(component.getEmailErrorMessage()).toBe('Please enter a valid email address');
      
      emailControl?.setErrors(null);
      expect(component.getEmailErrorMessage()).toBe('');
    });

    it('should return correct password error messages', () => {
      const passwordControl = component.loginForm.get('password');
      
      passwordControl?.setErrors({ required: true });
      expect(component.getPasswordErrorMessage()).toBe('Password is required');
      
      passwordControl?.setErrors({ minlength: { requiredLength: 8, actualLength: 5 } });
      expect(component.getPasswordErrorMessage()).toBe('Password must be at least 8 characters');
      
      passwordControl?.setErrors(null);
      expect(component.getPasswordErrorMessage()).toBe('');
    });
  });

  describe('Navigation', () => {
    it('should redirect to dashboard if already authenticated', () => {
      authStateSubject.next({
        isAuthenticated: true,
        user: { id: '1', email: 'test@example.com' } as any,
        tokens: { accessToken: 'token' } as any,
        loading: false,
        error: null
      });

      component.ngOnInit();

      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });
  });

  describe('Cleanup', () => {
    it('should unsubscribe on destroy', () => {
      spyOn(component['destroy$'], 'next');
      spyOn(component['destroy$'], 'complete');
      
      component.ngOnDestroy();
      
      expect(component['destroy$'].next).toHaveBeenCalled();
      expect(component['destroy$'].complete).toHaveBeenCalled();
    });
  });
});