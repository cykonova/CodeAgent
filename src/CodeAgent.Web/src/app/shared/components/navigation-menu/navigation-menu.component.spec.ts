import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router, NavigationEnd } from '@angular/router';
import { of, Subject } from 'rxjs';
import { NavigationMenuComponent } from './navigation-menu.component';
import { MenuItem, MenuSection } from '@shared/models/navigation.model';

describe('NavigationMenuComponent', () => {
  let component: NavigationMenuComponent;
  let fixture: ComponentFixture<NavigationMenuComponent>;
  let router: Router;
  let routerEventsSubject: Subject<any>;
  
  const mockMenuSections: MenuSection[] = [
    {
      title: 'Test Section',
      items: [
        { label: 'Item 1', icon: 'home', route: '/item1' },
        { 
          label: 'Item 2', 
          icon: 'folder', 
          route: '/item2',
          children: [
            { label: 'Child 1', icon: 'file', route: '/item2/child1' },
            { label: 'Child 2', icon: 'file', route: '/item2/child2' }
          ]
        }
      ],
      collapsible: true,
      defaultExpanded: true
    }
  ];

  beforeEach(async () => {
    routerEventsSubject = new Subject();
    
    await TestBed.configureTestingModule({
      imports: [
        NavigationMenuComponent,
        RouterTestingModule,
        NoopAnimationsModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NavigationMenuComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    
    // Mock router events
    Object.defineProperty(router, 'events', {
      get: () => routerEventsSubject.asObservable()
    });
    
    // Mock router.url
    Object.defineProperty(router, 'url', {
      get: () => '/dashboard'
    });
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default menu sections if none provided', () => {
    fixture.detectChanges();
    expect(component.menuItems.length).toBeGreaterThan(0);
  });

  it('should use provided menu items', () => {
    component.menuItems = mockMenuSections;
    fixture.detectChanges();
    expect(component.menuItems).toEqual(mockMenuSections);
  });

  it('should emit itemClick event when item is clicked', () => {
    const item: MenuItem = { label: 'Test', icon: 'test', route: '/test' };
    spyOn(component.itemClick, 'emit');
    
    component.onItemClick(item);
    
    expect(component.itemClick.emit).toHaveBeenCalledWith(item);
  });

  it('should not emit itemClick for disabled items', () => {
    const item: MenuItem = { label: 'Test', icon: 'test', route: '/test', disabled: true };
    spyOn(component.itemClick, 'emit');
    
    component.onItemClick(item);
    
    expect(component.itemClick.emit).not.toHaveBeenCalled();
  });

  it('should toggle section expansion', () => {
    const section: MenuSection = {
      title: 'Test',
      items: [],
      collapsible: true,
      expanded: false
    };
    
    component.toggleSection(section);
    expect(section.expanded).toBe(true);
    
    component.toggleSection(section);
    expect(section.expanded).toBe(false);
  });

  it('should not toggle non-collapsible sections', () => {
    const section: MenuSection = {
      title: 'Test',
      items: [],
      collapsible: false,
      expanded: true
    };
    
    component.toggleSection(section);
    expect(section.expanded).toBe(true);
  });

  it('should auto-collapse other sections when autoCollapse is enabled', () => {
    component.autoCollapse = true;
    const section1: MenuSection = {
      title: 'Section 1',
      items: [],
      collapsible: true,
      expanded: true
    };
    const section2: MenuSection = {
      title: 'Section 2',
      items: [],
      collapsible: true,
      expanded: false
    };
    
    component.menuItems = [section1, section2];
    fixture.detectChanges();
    
    component.toggleSection(section2);
    
    expect(section1.expanded).toBe(false);
    expect(section2.expanded).toBe(true);
  });

  it('should detect active route correctly', () => {
    component.activeRoute = '/dashboard';
    const item: MenuItem = { label: 'Dashboard', icon: 'dashboard', route: '/dashboard' };
    
    expect(component.isItemActive(item)).toBe(true);
  });

  it('should detect active child routes', () => {
    component.activeRoute = '/agents/running';
    const item: MenuItem = {
      label: 'Agents',
      icon: 'smart_toy',
      route: '/agents',
      children: [
        { label: 'Running', icon: 'play', route: '/agents/running' }
      ]
    };
    
    expect(component.isChildActive(item)).toBe(true);
  });

  it('should expand parent items with active children', () => {
    const item: MenuItem = {
      label: 'Parent',
      icon: 'folder',
      route: '/parent',
      expanded: false,
      children: [
        { label: 'Child', icon: 'file', route: '/parent/child' }
      ]
    };
    
    component.menuItems = [{
      title: 'Test',
      items: [item]
    }];
    
    component.activeRoute = '/parent/child';
    component['updateExpandedStates']();
    
    expect(item.expanded).toBe(true);
  });

  it('should toggle item expansion for items with children', () => {
    const item: MenuItem = {
      label: 'Parent',
      icon: 'folder',
      children: [
        { label: 'Child', icon: 'file', route: '/child' }
      ],
      expanded: false
    };
    
    component.onItemClick(item);
    expect(item.expanded).toBe(true);
    
    component.onItemClick(item);
    expect(item.expanded).toBe(false);
  });

  it('should navigate when item with route is clicked', () => {
    const item: MenuItem = { label: 'Test', icon: 'test', route: '/test' };
    spyOn(router, 'navigate');
    
    component.onItemClick(item);
    
    expect(router.navigate).toHaveBeenCalledWith(['/test']);
  });

  it('should return tooltip text only when collapsed', () => {
    const item: MenuItem = { label: 'Test Item', icon: 'test' };
    
    component.collapsed = false;
    expect(component.getItemTooltip(item)).toBe('');
    
    component.collapsed = true;
    expect(component.getItemTooltip(item)).toBe('Test Item');
  });

  it('should handle permission checks', () => {
    const itemWithoutPermissions: MenuItem = { label: 'Test', icon: 'test' };
    const itemWithPermissions: MenuItem = { 
      label: 'Test', 
      icon: 'test',
      permissions: ['admin']
    };
    
    expect(component.hasPermission(itemWithoutPermissions)).toBe(true);
    // TODO: When permission service is implemented, this should check actual permissions
    expect(component.hasPermission(itemWithPermissions)).toBe(true);
  });

  it('should update active route on navigation', () => {
    fixture.detectChanges();
    
    const navigationEnd = new NavigationEnd(1, '/new-route', '/new-route');
    routerEventsSubject.next(navigationEnd);
    
    expect(component.activeRoute).toBe('/new-route');
  });

  it('should clean up subscriptions on destroy', () => {
    fixture.detectChanges();
    spyOn(component['destroy$'], 'next');
    spyOn(component['destroy$'], 'complete');
    
    component.ngOnDestroy();
    
    expect(component['destroy$'].next).toHaveBeenCalled();
    expect(component['destroy$'].complete).toHaveBeenCalled();
  });
});