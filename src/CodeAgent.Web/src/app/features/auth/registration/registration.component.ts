import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil, debounceTime } from 'rxjs';

// Material imports
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';

import { AuthService } from '../../../core/services/auth.service';
import { RegistrationData } from '../../../core/models/auth.models';

interface PasswordStrength {
  score: number;
  label: string;
  color: string;
  percentage: number;
}

@Component({
  selector: 'app-registration',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDividerModule
  ],
  templateUrl: './registration.component.html',
  styleUrl: './registration.component.scss'
})
export class RegistrationComponent implements OnInit, OnDestroy {
  registrationForm!: FormGroup;
  hidePassword = signal(true);
  hideConfirmPassword = signal(true);
  isSubmitting = signal(false);
  passwordStrength = signal<PasswordStrength>({
    score: 0,
    label: 'Weak',
    color: 'warn',
    percentage: 0
  });

  private destroy$ = new Subject<void>();

  // Password requirements
  readonly passwordRequirements = [
    { label: 'At least 8 characters', regex: /.{8,}/ },
    { label: 'Contains uppercase letter', regex: /[A-Z]/ },
    { label: 'Contains lowercase letter', regex: /[a-z]/ },
    { label: 'Contains number', regex: /[0-9]/ },
    { label: 'Contains special character', regex: /[!@#$%^&*(),.?":{}|<>]/ }
  ];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.setupPasswordStrengthMonitor();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.registrationForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email], [this.emailAvailabilityValidator.bind(this)]],
      password: ['', [Validators.required, this.passwordValidator.bind(this)]],
      confirmPassword: ['', [Validators.required]],
      acceptTerms: [false, [Validators.requiredTrue]]
    }, {
      validators: [this.passwordMatchValidator]
    });
  }

  private setupPasswordStrengthMonitor(): void {
    this.registrationForm.get('password')?.valueChanges
      .pipe(
        debounceTime(300),
        takeUntil(this.destroy$)
      )
      .subscribe(password => {
        this.passwordStrength.set(this.calculatePasswordStrength(password));
      });
  }

  private calculatePasswordStrength(password: string): PasswordStrength {
    if (!password) {
      return { score: 0, label: 'Weak', color: 'warn', percentage: 0 };
    }

    let score = 0;
    
    // Length scoring
    if (password.length >= 8) score += 20;
    if (password.length >= 12) score += 10;
    if (password.length >= 16) score += 10;
    
    // Character variety scoring
    if (/[a-z]/.test(password)) score += 15;
    if (/[A-Z]/.test(password)) score += 15;
    if (/[0-9]/.test(password)) score += 15;
    if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) score += 15;
    
    // Pattern detection (reduce score for common patterns)
    if (/(.)\1{2,}/.test(password)) score -= 10; // Repeated characters
    if (/^[0-9]+$/.test(password)) score -= 10; // Only numbers
    if (/^[a-zA-Z]+$/.test(password)) score -= 10; // Only letters
    
    // Common passwords check (simplified)
    const commonPasswords = ['password', '12345678', 'qwerty', 'abc123', 'password123'];
    if (commonPasswords.some(common => password.toLowerCase().includes(common))) {
      score = Math.min(score, 25);
    }
    
    // Ensure score is between 0 and 100
    score = Math.max(0, Math.min(100, score));
    
    // Determine strength label and color
    let label: string;
    let color: string;
    
    if (score < 30) {
      label = 'Weak';
      color = 'warn';
    } else if (score < 50) {
      label = 'Fair';
      color = 'accent';
    } else if (score < 70) {
      label = 'Good';
      color = 'primary';
    } else {
      label = 'Strong';
      color = 'success';
    }
    
    return { score, label, color, percentage: score };
  }

  private passwordValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.value;
    if (!password) return null;

    const errors: ValidationErrors = {};
    
    if (password.length < 8) {
      errors['minLength'] = true;
    }
    
    if (!/[A-Z]/.test(password)) {
      errors['uppercase'] = true;
    }
    
    if (!/[a-z]/.test(password)) {
      errors['lowercase'] = true;
    }
    
    if (!/[0-9]/.test(password)) {
      errors['number'] = true;
    }
    
    if (!/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
      errors['special'] = true;
    }
    
    return Object.keys(errors).length > 0 ? errors : null;
  }

  private passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    
    if (!password || !confirmPassword) return null;
    
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  private async emailAvailabilityValidator(control: AbstractControl): Promise<ValidationErrors | null> {
    const email = control.value;
    if (!email || control.errors?.['email']) return null;
    
    // Simulate API call to check email availability
    // In production, this would call an actual API endpoint
    return new Promise((resolve) => {
      setTimeout(() => {
        // For demo purposes, reject some common emails
        const takenEmails = ['admin@example.com', 'test@example.com'];
        resolve(takenEmails.includes(email.toLowerCase()) ? { emailTaken: true } : null);
      }, 500);
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update(value => !value);
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.update(value => !value);
  }

  getPasswordRequirementStatus(requirement: { regex: RegExp }): boolean {
    const password = this.registrationForm.get('password')?.value || '';
    return requirement.regex.test(password);
  }

  getPasswordStrengthTooltip(): string {
    return `Password Strength: ${this.passwordStrength().label} (${this.passwordStrength().percentage}%)
    
Requirements:
${this.passwordRequirements.map(req => 
  `${this.getPasswordRequirementStatus(req) ? '✓' : '✗'} ${req.label}`
).join('\n')}`;
  }

  async onSubmit(): Promise<void> {
    if (this.registrationForm.invalid) {
      // Mark all fields as touched to show validation errors
      Object.keys(this.registrationForm.controls).forEach(key => {
        this.registrationForm.get(key)?.markAsTouched();
      });
      
      this.snackBar.open('Please fix the errors in the form', 'Close', {
        duration: 3000,
        horizontalPosition: 'center',
        verticalPosition: 'top',
        panelClass: ['error-snackbar']
      });
      return;
    }

    this.isSubmitting.set(true);
    
    const registrationData: RegistrationData = {
      firstName: this.registrationForm.value.firstName,
      lastName: this.registrationForm.value.lastName,
      email: this.registrationForm.value.email,
      password: this.registrationForm.value.password,
      acceptTerms: this.registrationForm.value.acceptTerms
    };

    this.authService.register(registrationData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open(
            'Registration successful! Please check your email to verify your account.',
            'Close',
            {
              duration: 5000,
              horizontalPosition: 'center',
              verticalPosition: 'top',
              panelClass: ['success-snackbar']
            }
          );
          // Navigation is handled by the auth service
        },
        error: (error) => {
          this.isSubmitting.set(false);
          this.snackBar.open(
            error.message || 'Registration failed. Please try again.',
            'Close',
            {
              duration: 5000,
              horizontalPosition: 'center',
              verticalPosition: 'top',
              panelClass: ['error-snackbar']
            }
          );
        },
        complete: () => {
          this.isSubmitting.set(false);
        }
      });
  }

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }

  openTerms(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    window.open('/terms', '_blank');
  }

  openPrivacy(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    window.open('/privacy', '_blank');
  }
}