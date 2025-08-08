import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { ThemeService } from '../../../services/theme';

interface NavItem {
  path: string;
  icon: string;
  label: string;
  tooltip?: string;
}

@Component({
  selector: 'app-main-layout',
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDividerModule
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {
  private themeService = inject(ThemeService);
  
  isDark = computed(() => this.themeService.currentTheme() === 'dark');
  showRightPanel = false;
  
  navItems: NavItem[] = [
    { path: '/chat', icon: 'chat', label: 'Chat', tooltip: 'AI Chat Assistant' },
    { path: '/files', icon: 'folder', label: 'Files', tooltip: 'Browse Files' },
    { path: '/config', icon: 'settings', label: 'Configuration', tooltip: 'Settings' },
    { path: '/about', icon: 'info', label: 'About', tooltip: 'About CodeAgent' }
  ];
  
  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
  
  toggleRightPanel(): void {
    this.showRightPanel = !this.showRightPanel;
  }
}