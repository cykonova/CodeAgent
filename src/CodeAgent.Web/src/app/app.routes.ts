import { Routes } from '@angular/router';

export const routes: Routes = [
  { 
    path: '', 
    loadComponent: () => import('./features/dashboard/dashboard-home/dashboard-home.component')
      .then(m => m.DashboardHomeComponent)
  }
];
