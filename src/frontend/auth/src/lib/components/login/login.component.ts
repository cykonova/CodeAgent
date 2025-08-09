import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);
  
  loginForm!: FormGroup;
  isLoading = false;
  hidePassword = true;
  returnUrl = '/dashboard';
  
  ngOnInit(): void {
    this.initializeForm();
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
    
    // Check if already authenticated
    this.authService.isAuthenticated$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.router.navigate([this.returnUrl]);
      }
    });
  }
  
  private initializeForm(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      rememberMe: [false]
    });
  }
  
  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.markFormGroupTouched(this.loginForm);
      return;
    }
    
    this.isLoading = true;
    const loginData = this.loginForm.value;
    
    this.authService.login(loginData)
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: (response) => {
          this.snackBar.open('Login successful!', 'Close', {
            duration: 3000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: 'success-snackbar'
          });
          
          const redirectUrl = sessionStorage.getItem('redirectUrl') || this.returnUrl;
          sessionStorage.removeItem('redirectUrl');
          this.router.navigate([redirectUrl]);
        },
        error: (error) => {
          const message = error.error?.message || 'Invalid email or password';
          this.snackBar.open(message, 'Close', {
            duration: 5000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: 'error-snackbar'
          });
        }
      });
  }
  
  forgotPassword(): void {
    this.router.navigate(['/forgot-password']);
  }
  
  register(): void {
    this.router.navigate(['/register']);
  }
  
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
      
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
  
  getEmailErrorMessage(): string {
    const email = this.loginForm.get('email');
    if (email?.hasError('required')) {
      return 'Email is required';
    }
    if (email?.hasError('email')) {
      return 'Please enter a valid email';
    }
    return '';
  }
  
  getPasswordErrorMessage(): string {
    const password = this.loginForm.get('password');
    if (password?.hasError('required')) {
      return 'Password is required';
    }
    if (password?.hasError('minlength')) {
      return 'Password must be at least 8 characters';
    }
    return '';
  }
}