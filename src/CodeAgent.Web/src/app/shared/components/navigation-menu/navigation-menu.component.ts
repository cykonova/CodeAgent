import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';

export interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: number | string;
  badgeColor?: 'primary' | 'accent' | 'warn';
  children?: NavItem[];
}

export interface NavSection {
  title: string;
  items: NavItem[];
}

@Component({
  selector: 'app-navigation-menu',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatListModule,
    MatIconModule,
    MatBadgeModule,
    MatDividerModule,
    MatTooltipModule
  ],
  templateUrl: './navigation-menu.component.html',
  styleUrl: './navigation-menu.component.scss'
})
export class NavigationMenuComponent {
  @Input() collapsed = false;
  @Output() itemClick = new EventEmitter<NavItem>();
  
  navSections: NavSection[] = [
    {
      title: 'Main',
      items: [
        { label: 'Dashboard', icon: 'dashboard', route: '/dashboard' }
      ]
    },
    {
      title: 'Projects',
      items: [
        { label: 'All Projects', icon: 'folder', route: '/projects' },
        { label: 'Create New', icon: 'add_circle', route: '/projects/new' },
        { label: 'Templates', icon: 'content_copy', route: '/projects/templates' }
      ]
    },
    {
      title: 'Agents',
      items: [
        { label: 'Running Agents', icon: 'smart_toy', route: '/agents', badge: '2', badgeColor: 'accent' },
        { label: 'Agent Types', icon: 'category', route: '/agents/types' },
        { label: 'Logs', icon: 'history', route: '/agents/logs' }
      ]
    },
    {
      title: 'Settings',
      items: [
        { label: 'Profile', icon: 'person', route: '/settings/profile' },
        { label: 'Preferences', icon: 'tune', route: '/settings/preferences' },
        { label: 'API Keys', icon: 'vpn_key', route: '/settings/api-keys' }
      ]
    }
  ];
  
  onItemClick(item: NavItem): void {
    this.itemClick.emit(item);
  }
}