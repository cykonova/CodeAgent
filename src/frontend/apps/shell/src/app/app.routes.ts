import { Route } from '@angular/router';
import { loadRemoteModule } from '@nx/angular/mf';
import { authGuard, LoginComponent, RegisterComponent } from '@code-agent/auth';

export const appRoutes: Route[] = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  },
  {
    path: 'chat',
    loadChildren: () =>
      loadRemoteModule('chat', './Module').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    loadChildren: () =>
      loadRemoteModule('dashboard', './Module').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: 'projects',
    loadChildren: () =>
      loadRemoteModule('projects', './Module').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: 'settings',
    loadChildren: () =>
      loadRemoteModule('settings', './Module').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  }
];
