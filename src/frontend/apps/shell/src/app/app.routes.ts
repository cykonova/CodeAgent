import { Route } from '@angular/router';
import { authGuard, LoginComponent, RegisterComponent } from '@code-agent/feature/auth';

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
      import('chat/Module').then(m => m.RemoteEntryModule),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    loadChildren: () =>
      import('dashboard/Module').then(m => m.RemoteEntryModule),
    canActivate: [authGuard]
  },
  {
    path: 'projects',
    loadChildren: () =>
      import('projects/Module').then(m => m.RemoteEntryModule),
    canActivate: [authGuard]
  },
  {
    path: 'settings',
    loadChildren: () =>
      import('settings/Module').then(m => m.RemoteEntryModule),
    canActivate: [authGuard]
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  }
];
