import { Route } from '@angular/router';
import { loadRemoteModule } from '@nx/angular/mf';

export const appRoutes: Route[] = [
  {
    path: 'chat',
    loadChildren: () =>
      loadRemoteModule('chat', './Module').then(m => m.App)
  },
  {
    path: 'dashboard',
    loadChildren: () =>
      loadRemoteModule('dashboard', './Module').then(m => m.App)
  },
  {
    path: 'projects',
    loadChildren: () =>
      loadRemoteModule('projects', './Module').then(m => m.App)
  },
  {
    path: 'settings',
    loadChildren: () =>
      loadRemoteModule('settings', './Module').then(m => m.App)
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  }
];
