import { Route } from '@angular/router';
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
    loadComponent: () =>
      import('../../../../chat/src/app/app').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('../../../../dashboard/src/app/app').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: 'projects',
    loadComponent: () =>
      import('../../../../projects/src/app/app').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('../../../../settings/src/app/app').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  }
];
