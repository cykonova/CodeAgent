import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component')
      .then(m => m.LoginComponent)
  },
  {
    path: 'auth',
    children: [
      {
        path: 'register',
        loadComponent: () => import('./features/auth/login/login.component')
          .then(m => m.LoginComponent) // Placeholder - will be replaced with RegisterComponent
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('./features/auth/login/login.component')
          .then(m => m.LoginComponent) // Placeholder - will be replaced with ForgotPasswordComponent
      },
      {
        path: 'verify-email',
        loadComponent: () => import('./features/auth/login/login.component')
          .then(m => m.LoginComponent) // Placeholder - will be replaced with VerifyEmailComponent
      }
    ]
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard-home/dashboard-home.component')
      .then(m => m.DashboardHomeComponent),
    canActivate: [authGuard]
  },
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];
