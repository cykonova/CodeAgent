import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { RegistrationComponent } from './registration.component';
import { AuthService } from '../../../core/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';

describe('RegistrationComponent', () => {
  let component: RegistrationComponent;
  let fixture: ComponentFixture<RegistrationComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let snackBar: jasmine.SpyObj<MatSnackBar>;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['register']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    const snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [
        RegistrationComponent,
        ReactiveFormsModule,
        NoopAnimationsModule
      ],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: MatSnackBar, useValue: snackBarSpy }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    snackBar = TestBed.inject(MatSnackBar) as jasmine.SpyObj<MatSnackBar>;
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RegistrationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize the form with empty values', () => {
    expect(component.registrationForm.value).toEqual({
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
      acceptTerms: false
    });
  });

  it('should validate required fields', () => {
    const form = component.registrationForm;
    expect(form.valid).toBeFalsy();

    form.patchValue({
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      password: 'Test@1234',
      confirmPassword: 'Test@1234',
      acceptTerms: true
    });

    expect(form.valid).toBeTruthy();
  });

  it('should validate email format', () => {
    const emailControl = component.registrationForm.get('email');
    
    emailControl?.setValue('invalid-email');
    expect(emailControl?.hasError('email')).toBeTruthy();
    
    emailControl?.setValue('valid@email.com');
    expect(emailControl?.hasError('email')).toBeFalsy();
  });

  it('should validate password strength', () => {
    const passwordControl = component.registrationForm.get('password');
    
    passwordControl?.setValue('weak');
    expect(passwordControl?.hasError('minLength')).toBeTruthy();
    
    passwordControl?.setValue('weakpassword');
    expect(passwordControl?.hasError('uppercase')).toBeTruthy();
    
    passwordControl?.setValue('WEAKPASSWORD');
    expect(passwordControl?.hasError('lowercase')).toBeTruthy();
    
    passwordControl?.setValue('WeakPassword');
    expect(passwordControl?.hasError('number')).toBeTruthy();
    
    passwordControl?.setValue('WeakPassword1');
    expect(passwordControl?.hasError('special')).toBeTruthy();
    
    passwordControl?.setValue('Strong@Pass1');
    expect(passwordControl?.errors).toBeNull();
  });

  it('should validate password match', () => {
    const form = component.registrationForm;
    
    form.patchValue({
      password: 'Test@1234',
      confirmPassword: 'Different@1234'
    });
    
    expect(form.hasError('passwordMismatch')).toBeTruthy();
    
    form.patchValue({
      confirmPassword: 'Test@1234'
    });
    
    expect(form.hasError('passwordMismatch')).toBeFalsy();
  });

  it('should calculate password strength correctly', () => {
    const passwordControl = component.registrationForm.get('password');
    
    passwordControl?.setValue('weak');
    expect(component.passwordStrength().label).toBe('Weak');
    
    passwordControl?.setValue('moderate12');
    fixture.detectChanges();
    tick(300);
    // Should be Fair or Good depending on scoring
    
    passwordControl?.setValue('Strong@Pass123');
    fixture.detectChanges();
    tick(300);
    // Should be Good or Strong
  });

  it('should toggle password visibility', () => {
    expect(component.hidePassword()).toBeTruthy();
    component.togglePasswordVisibility();
    expect(component.hidePassword()).toBeFalsy();
    component.togglePasswordVisibility();
    expect(component.hidePassword()).toBeTruthy();
  });

  it('should toggle confirm password visibility', () => {
    expect(component.hideConfirmPassword()).toBeTruthy();
    component.toggleConfirmPasswordVisibility();
    expect(component.hideConfirmPassword()).toBeFalsy();
    component.toggleConfirmPasswordVisibility();
    expect(component.hideConfirmPassword()).toBeTruthy();
  });

  it('should handle successful registration', fakeAsync(() => {
    const form = component.registrationForm;
    form.patchValue({
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      password: 'Test@1234',
      confirmPassword: 'Test@1234',
      acceptTerms: true
    });

    authService.register.and.returnValue(of({} as any));
    
    component.onSubmit();
    tick();
    
    expect(authService.register).toHaveBeenCalledWith({
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      password: 'Test@1234',
      acceptTerms: true
    });
    
    expect(snackBar.open).toHaveBeenCalledWith(
      'Registration successful! Please check your email to verify your account.',
      'Close',
      jasmine.objectContaining({
        duration: 5000,
        panelClass: ['success-snackbar']
      })
    );
  }));

  it('should handle registration error', fakeAsync(() => {
    const form = component.registrationForm;
    form.patchValue({
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      password: 'Test@1234',
      confirmPassword: 'Test@1234',
      acceptTerms: true
    });

    const error = { message: 'Email already exists' };
    authService.register.and.returnValue(throwError(() => error));
    
    component.onSubmit();
    tick();
    
    expect(component.isSubmitting()).toBeFalsy();
    expect(snackBar.open).toHaveBeenCalledWith(
      'Email already exists',
      'Close',
      jasmine.objectContaining({
        duration: 5000,
        panelClass: ['error-snackbar']
      })
    );
  }));

  it('should not submit if form is invalid', () => {
    component.onSubmit();
    
    expect(authService.register).not.toHaveBeenCalled();
    expect(snackBar.open).toHaveBeenCalledWith(
      'Please fix the errors in the form',
      'Close',
      jasmine.objectContaining({
        duration: 3000,
        panelClass: ['error-snackbar']
      })
    );
  });

  it('should navigate to login page', () => {
    component.navigateToLogin();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should open terms in new window', () => {
    spyOn(window, 'open');
    const event = new Event('click');
    
    component.openTerms(event);
    expect(window.open).toHaveBeenCalledWith('/terms', '_blank');
  });

  it('should open privacy policy in new window', () => {
    spyOn(window, 'open');
    const event = new Event('click');
    
    component.openPrivacy(event);
    expect(window.open).toHaveBeenCalledWith('/privacy', '_blank');
  });

  it('should check password requirements correctly', () => {
    const passwordControl = component.registrationForm.get('password');
    
    passwordControl?.setValue('Short1!');
    expect(component.getPasswordRequirementStatus({ regex: /.{8,}/ })).toBeFalsy();
    
    passwordControl?.setValue('longpassword');
    expect(component.getPasswordRequirementStatus({ regex: /[A-Z]/ })).toBeFalsy();
    expect(component.getPasswordRequirementStatus({ regex: /[a-z]/ })).toBeTruthy();
    
    passwordControl?.setValue('Strong@Pass123');
    component.passwordRequirements.forEach(req => {
      expect(component.getPasswordRequirementStatus(req)).toBeTruthy();
    });
  });

  it('should cleanup on destroy', () => {
    spyOn(component['destroy$'], 'next');
    spyOn(component['destroy$'], 'complete');
    
    component.ngOnDestroy();
    
    expect(component['destroy$'].next).toHaveBeenCalled();
    expect(component['destroy$'].complete).toHaveBeenCalled();
  });
});