import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule, 
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    MatListModule,
    MatMenuModule,
    MatTooltipModule
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'Code Agent Portal';
  isSidenavOpen = true;
  isDarkTheme = false;

  navigationItems = [
    { path: '/dashboard', icon: 'dashboard', label: 'Dashboard' },
    { path: '/projects', icon: 'folder', label: 'Projects' },
    { path: '/chat', icon: 'chat', label: 'Chat' },
    { path: '/settings', icon: 'settings', label: 'Settings' }
  ];

  toggleSidenav(): void {
    this.isSidenavOpen = !this.isSidenavOpen;
  }

  toggleTheme(): void {
    this.isDarkTheme = !this.isDarkTheme;
    const body = document.body;
    
    if (this.isDarkTheme) {
      body.classList.add('dark-theme');
    } else {
      body.classList.remove('dark-theme');
    }
    
    // Save preference to localStorage
    localStorage.setItem('theme', this.isDarkTheme ? 'dark' : 'light');
  }

  ngOnInit(): void {
    // Load theme preference
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
      this.isDarkTheme = true;
      document.body.classList.add('dark-theme');
    }
  }
}