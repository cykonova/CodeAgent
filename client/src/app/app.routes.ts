import { Routes } from '@angular/router';
import { MainLayout } from './components/layout/main-layout/main-layout';

export const routes: Routes = [
  {
    path: '',
    component: MainLayout,
    children: [
      {
        path: '',
        redirectTo: 'chat',
        pathMatch: 'full'
      },
      {
        path: 'chat',
        loadComponent: () => import('./components/chat/chat-container/chat-container').then(m => m.ChatContainer)
      },
      {
        path: 'files',
        loadComponent: () => import('./components/files/file-browser/file-browser').then(m => m.FileBrowser)
      },
      {
        path: 'config',
        loadComponent: () => import('./components/configuration/config-panel/config-panel').then(m => m.ConfigPanel)
      },
      {
        path: 'about',
        loadComponent: () => import('./components/about/about/about').then(m => m.About)
      }
    ]
  }
];