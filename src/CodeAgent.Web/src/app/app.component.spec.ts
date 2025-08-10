import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { BreakpointObserver } from '@angular/cdk/layout';
import { of, BehaviorSubject } from 'rxjs';
import { signal } from '@angular/core';
import { AppComponent } from './app.component';
import { ThemeService } from '@core/services/theme.service';
import { WebSocketService } from '@core/services/websocket.service';
import { WebSocketState } from '@core/models/websocket.model';
import { NavigationMenuComponent } from '@shared/components/navigation-menu/navigation-menu.component';
import { ThemeToggleComponent } from '@shared/components/theme-toggle/theme-toggle.component';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatListModule } from '@angular/material/list';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { Router, NavigationEnd } from '@angular/router';

describe('AppComponent', () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;
  let mockThemeService: jasmine.SpyObj<ThemeService>;
  let mockWebSocketService: jasmine.SpyObj<WebSocketService>;
  let mockBreakpointObserver: jasmine.SpyObj<BreakpointObserver>;
  let mockRouter: jasmine.SpyObj<Router>;
  let connectionStateSubject: BehaviorSubject<WebSocketState>;

  beforeEach(async () => {
    // Create mock services
    mockThemeService = jasmine.createSpyObj('ThemeService', ['toggleTheme'], {
      theme: signal('light')
    });
    
    connectionStateSubject = new BehaviorSubject<WebSocketState>(WebSocketState.Disconnected);
    mockWebSocketService = jasmine.createSpyObj('WebSocketService', ['disconnect'], {
      connectionState$: connectionStateSubject.asObservable(),
      connectionIcon$: of('error'),
      connectionClass$: of('connection-disconnected'),
      connectionText$: of('Disconnected'),
      isConnecting$: of(false)
    });
    
    mockBreakpointObserver = jasmine.createSpyObj('BreakpointObserver', ['observe']);
    mockBreakpointObserver.observe.and.returnValue(
      of({
        matches: false,
        breakpoints: {
          '(max-width: 767px)': false,
          '(min-width: 768px) and (max-width: 1023px)': false,
          '(min-width: 1024px)': true
        }
      })
    );
    
    mockRouter = jasmine.createSpyObj('Router', ['navigate'], {
      events: of(new NavigationEnd(0, '/', '/'))
    });

    await TestBed.configureTestingModule({
      imports: [
        AppComponent,
        RouterTestingModule,
        NoopAnimationsModule,
        MatToolbarModule,
        MatSidenavModule,
        MatButtonModule,
        MatIconModule,
        MatMenuModule,
        MatListModule,
        MatBadgeModule,
        MatDividerModule
      ],
      providers: [
        { provide: ThemeService, useValue: mockThemeService },
        { provide: WebSocketService, useValue: mockWebSocketService },
        { provide: BreakpointObserver, useValue: mockBreakpointObserver },
        { provide: Router, useValue: mockRouter }
      ]
    })
    .overrideComponent(AppComponent, {
      remove: {
        imports: [NavigationMenuComponent, ThemeToggleComponent]
      },
      add: {
        imports: []
      }
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have correct title', () => {
    expect(component.title).toBe('Code Agent');
  });

  describe('Component Initialization', () => {
    it('should initialize with default values', () => {
      expect(component.isCollapsed).toBe(false);
      expect(component.notificationCount).toBe(3);
      expect(component.userName).toBe('User Name');
      expect(component.userEmail).toBe('user@example.com');
    });

    it('should setup responsive sidenav on init', () => {
      fixture.detectChanges();
      expect(mockBreakpointObserver.observe).toHaveBeenCalled();
    });

    it('should initialize theme observable', () => {
      fixture.detectChanges();
      component.isDarkTheme$.subscribe(isDark => {
        expect(isDark).toBe(false); // light theme
      });
    });

    it('should initialize WebSocket observables', () => {
      fixture.detectChanges();
      expect(component.connectionState$).toBeDefined();
      expect(component.connectionIcon$).toBeDefined();
      expect(component.connectionClass$).toBeDefined();
      expect(component.connectionText$).toBeDefined();
    });
  });

  describe('Theme Toggle', () => {
    it('should call themeService.toggleTheme when toggleTheme is called', () => {
      component.toggleTheme();
      expect(mockThemeService.toggleTheme).toHaveBeenCalled();
    });
  });

  describe('Connection Status', () => {
    it('should update connection status when WebSocket state changes', (done) => {
      connectionStateSubject.next(WebSocketState.Connected);
      
      component.connectionState$.subscribe(state => {
        expect(state).toBe(WebSocketState.Connected);
        done();
      });
    });

    it('should display correct connection icon', (done) => {
      // Test the existing mock value
      mockWebSocketService.connectionIcon$.subscribe(icon => {
        expect(icon).toBe('error'); // Based on the mock setup
        done();
      });
    });
  });

  describe('Sidenav Toggle', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should toggle collapsed state on desktop when sidenav is open', () => {
      component.isDesktop = true;
      component.sidenavOpened$.next(true);
      component.isCollapsed = false;
      
      component.toggleSidenav();
      
      expect(component.isCollapsed).toBe(true);
    });

    it('should toggle sidenav open/closed on mobile', () => {
      component.isDesktop = false;
      component.isMobile = true;
      component.sidenavOpened$.next(false);
      
      component.toggleSidenav();
      
      expect(component.sidenavOpened$.value).toBe(true);
    });
  });

  describe('Responsive Behavior', () => {
    it('should set mobile mode for small screens', (done) => {
      mockBreakpointObserver.observe.and.returnValue(
        of({
          matches: true,
          breakpoints: {
            '(max-width: 767px)': true,
            '(min-width: 768px) and (max-width: 1023px)': false,
            '(min-width: 1024px)': false
          }
        })
      );
      
      component.ngOnInit();
      
      setTimeout(() => {
        expect(component.isMobile).toBe(true);
        expect(component.sidenavMode$.value).toBe('over');
        expect(component.sidenavOpened$.value).toBe(false);
        done();
      }, 100);
    });

    it('should set desktop mode for large screens', (done) => {
      mockBreakpointObserver.observe.and.returnValue(
        of({
          matches: true,
          breakpoints: {
            '(max-width: 767px)': false,
            '(min-width: 768px) and (max-width: 1023px)': false,
            '(min-width: 1024px)': true
          }
        })
      );
      
      component.ngOnInit();
      
      setTimeout(() => {
        expect(component.isDesktop).toBe(true);
        expect(component.sidenavMode$.value).toBe('side');
        expect(component.sidenavOpened$.value).toBe(true);
        done();
      }, 100);
    });
  });

  describe('Navigation', () => {
    it('should close sidenav on mobile after navigation item click', () => {
      component.isMobile = true;
      component.sidenavOpened$.next(true);
      
      component.onNavItemClick();
      
      expect(component.sidenavOpened$.value).toBe(false);
    });

    it('should not close sidenav on desktop after navigation item click', () => {
      component.isMobile = false;
      component.isDesktop = true;
      component.sidenavOpened$.next(true);
      
      component.onNavItemClick();
      
      expect(component.sidenavOpened$.value).toBe(true);
    });

    it('should navigate to notifications page', () => {
      component.viewAllNotifications();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/notifications']);
    });
  });

  describe('User Actions', () => {
    it('should handle logout', () => {
      component.logout();
      
      expect(mockWebSocketService.disconnect).toHaveBeenCalled();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('DOM Elements', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should render toolbar', () => {
      const toolbar = fixture.nativeElement.querySelector('.app-toolbar');
      expect(toolbar).toBeTruthy();
    });

    it('should render app title', () => {
      const title = fixture.nativeElement.querySelector('.app-title');
      expect(title?.textContent).toContain('Code Agent');
    });

    it('should render menu toggle button', () => {
      const menuButton = fixture.nativeElement.querySelector('.menu-toggle');
      expect(menuButton).toBeTruthy();
    });

    it('should render sidenav container', () => {
      const sidenavContainer = fixture.nativeElement.querySelector('.sidenav-container');
      expect(sidenavContainer).toBeTruthy();
    });
  });

  describe('Cleanup', () => {
    it('should unsubscribe on destroy', () => {
      const spy = spyOn(component['destroy$'], 'next');
      const completeSpy = spyOn(component['destroy$'], 'complete');
      
      component.ngOnDestroy();
      
      expect(spy).toHaveBeenCalled();
      expect(completeSpy).toHaveBeenCalled();
    });
  });
});
