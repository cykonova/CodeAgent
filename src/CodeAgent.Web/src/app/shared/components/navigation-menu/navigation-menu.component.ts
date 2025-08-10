import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatButtonModule } from '@angular/material/button';
import { Subject, filter, takeUntil } from 'rxjs';
import { MenuItem, MenuSection } from '@shared/models/navigation.model';

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
    MatTooltipModule,
    MatExpansionModule,
    MatButtonModule
  ],
  templateUrl: './navigation-menu.component.html',
  styleUrl: './navigation-menu.component.scss'
})
export class NavigationMenuComponent implements OnInit, OnDestroy {
  @Input() menuItems: MenuSection[] = [];
  @Input() collapsed = false;
  @Input() showIcons = true;
  @Input() showBadges = true;
  @Input() allowNesting = true;
  @Input() autoCollapse = false;
  
  @Output() itemClick = new EventEmitter<MenuItem>();
  @Output() sectionToggle = new EventEmitter<MenuSection>();
  
  private destroy$ = new Subject<void>();
  activeRoute = '';
  expandedSections: Set<string> = new Set();
  expandedItems: Set<string> = new Set();
  
  // Default menu structure
  defaultMenuSections: MenuSection[] = [
    {
      title: 'Main',
      items: [
        { label: 'Dashboard', icon: 'dashboard', route: '/dashboard' },
        { label: 'Projects', icon: 'folder', route: '/projects', badge: '5' }
      ]
    },
    {
      title: 'Management',
      items: [
        { 
          label: 'Agents', 
          icon: 'smart_toy', 
          route: '/agents',
          expanded: false,
          children: [
            { label: 'Running', icon: 'play_circle', route: '/agents/running', badge: '3', badgeColor: 'primary' },
            { label: 'Types', icon: 'category', route: '/agents/types' },
            { label: 'Logs', icon: 'history', route: '/agents/logs' }
          ]
        },
        { label: 'Providers', icon: 'dns', route: '/providers' },
        { label: 'Workflows', icon: 'account_tree', route: '/workflows' }
      ]
    },
    {
      title: 'Settings',
      items: [
        { label: 'Profile', icon: 'person', route: '/settings/profile' },
        { label: 'Preferences', icon: 'settings', route: '/settings/preferences' },
        { label: 'API Keys', icon: 'vpn_key', route: '/settings/api-keys' }
      ]
    }
  ];
  
  constructor(private router: Router) {}
  
  ngOnInit(): void {
    // Use provided menu items or default
    if (!this.menuItems || this.menuItems.length === 0) {
      this.menuItems = this.defaultMenuSections;
    }
    
    // Initialize expanded states
    this.menuItems.forEach(section => {
      if (section.defaultExpanded || section.expanded) {
        this.expandedSections.add(section.title);
      }
      section.expanded = section.defaultExpanded !== false;
    });
    
    // Subscribe to router events for active route detection
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((event: NavigationEnd) => {
        this.activeRoute = event.urlAfterRedirects;
        this.updateExpandedStates();
      });
    
    // Set initial active route
    this.activeRoute = this.router.url;
    this.updateExpandedStates();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  onItemClick(item: MenuItem): void {
    if (item.disabled) {
      return;
    }
    
    // Handle items with children
    if (item.children && item.children.length > 0) {
      this.toggleItemExpansion(item);
    }
    
    // Navigate if route is provided
    if (item.route) {
      this.router.navigate([item.route]);
    }
    
    this.itemClick.emit(item);
  }
  
  toggleSection(section: MenuSection): void {
    if (!section.collapsible) {
      return;
    }
    
    section.expanded = !section.expanded;
    
    if (section.expanded) {
      this.expandedSections.add(section.title);
      
      // Auto-collapse other sections if enabled
      if (this.autoCollapse) {
        this.menuItems.forEach(otherSection => {
          if (otherSection !== section && otherSection.collapsible) {
            otherSection.expanded = false;
            this.expandedSections.delete(otherSection.title);
          }
        });
      }
    } else {
      this.expandedSections.delete(section.title);
    }
    
    this.sectionToggle.emit(section);
  }
  
  toggleItemExpansion(item: MenuItem): void {
    item.expanded = !item.expanded;
    const itemKey = this.getItemKey(item);
    
    if (item.expanded) {
      this.expandedItems.add(itemKey);
    } else {
      this.expandedItems.delete(itemKey);
    }
  }
  
  isItemActive(item: MenuItem): boolean {
    if (!item.route) {
      return false;
    }
    
    // Check exact match
    if (this.activeRoute === item.route) {
      return true;
    }
    
    // Check if current route starts with item route (for nested routes)
    if (item.children && item.children.length > 0) {
      return this.activeRoute.startsWith(item.route);
    }
    
    return false;
  }
  
  isChildActive(item: MenuItem): boolean {
    if (!item.children) {
      return false;
    }
    
    return item.children.some(child => this.isItemActive(child));
  }
  
  hasPermission(item: MenuItem): boolean {
    if (!item.permissions || item.permissions.length === 0) {
      return true;
    }
    
    // TODO: Implement actual permission check
    // This would typically check against a user service
    return true;
  }
  
  getItemTooltip(item: MenuItem): string {
    if (!this.collapsed) {
      return '';
    }
    
    return item.label;
  }
  
  trackBySection(index: number, section: MenuSection): string {
    return section.title;
  }
  
  trackByItem(index: number, item: MenuItem): string {
    return item.route || item.label;
  }
  
  private updateExpandedStates(): void {
    // Auto-expand sections containing active items
    this.menuItems.forEach(section => {
      const hasActiveItem = section.items.some(item => 
        this.isItemActive(item) || this.isChildActive(item)
      );
      
      if (hasActiveItem && section.collapsible) {
        section.expanded = true;
        this.expandedSections.add(section.title);
      }
      
      // Auto-expand parent items with active children
      section.items.forEach(item => {
        if (item.children && this.isChildActive(item)) {
          item.expanded = true;
          this.expandedItems.add(this.getItemKey(item));
        }
      });
    });
  }
  
  private getItemKey(item: MenuItem): string {
    return item.route || item.label;
  }
}