import { Component, OnInit, OnDestroy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, RouterLink, RouterLinkActive, NavigationStart, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { BreakpointObserver } from '@angular/cdk/layout';
import { ThemeToggleComponent } from '@shared/components/theme-toggle/theme-toggle.component';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatListModule } from '@angular/material/list';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { Subject, BehaviorSubject, Observable } from 'rxjs';
import { takeUntil, filter, map } from 'rxjs/operators';
import { toObservable } from '@angular/core/rxjs-interop';
import { MatSidenav } from '@angular/material/sidenav';
import { ThemeService } from '@core/services/theme.service';
import { WebSocketService, ConnectionState } from '@core/services/websocket.service';
import { NavigationMenuComponent } from '@shared/components/navigation-menu/navigation-menu.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ThemeToggleComponent,
    NavigationMenuComponent,
    MatToolbarModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatListModule,
    MatBadgeModule,
    MatDividerModule
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Code Agent';
  
  // ViewChild for sidenav reference
  @ViewChild('sidenav') sidenav!: MatSidenav;
  
  // Sidenav state
  sidenavOpened$ = new BehaviorSubject<boolean>(true);
  sidenavMode$ = new BehaviorSubject<'side' | 'over' | 'push'>('side');
  isCollapsed = false;
  
  // Theme state
  isDarkTheme$: Observable<boolean>;
  
  // WebSocket connection state
  connectionState$: Observable<ConnectionState>;
  connectionIcon$: Observable<string>;
  connectionClass$: Observable<string>;
  connectionText$: Observable<string>;
  isConnecting$: Observable<boolean>;
  
  // Notifications
  notificationCount = 3;
  notifications = [
    { type: 'info', icon: 'info', message: 'Agent completed successfully' },
    { type: 'warning', icon: 'warning', message: 'Build failed in project X' },
    { type: 'success', icon: 'check_circle', message: 'Deployment completed' }
  ];
  
  // User information
  userName = 'User Name';
  userEmail = 'user@example.com';
  
  // Responsive state
  isMobile = false;
  isTablet = false;
  isDesktop = true;
  
  private destroy$ = new Subject<void>();
  
  constructor(
    private breakpointObserver: BreakpointObserver,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private themeService: ThemeService,
    private webSocketService: WebSocketService
  ) {
    // Initialize theme observable
    this.isDarkTheme$ = toObservable(this.themeService.theme).pipe(
      map(theme => theme === 'dark')
    );
    
    // Initialize WebSocket observables
    this.connectionState$ = this.webSocketService.connectionState;
    this.connectionIcon$ = this.webSocketService.connectionIcon$;
    this.connectionClass$ = this.webSocketService.connectionClass$;
    this.connectionText$ = this.webSocketService.connectionText$;
    this.isConnecting$ = this.webSocketService.isConnecting$;
  }
  
  ngOnInit(): void {
    this.setupResponsiveSidenav();
    this.setupRouterEvents();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  /**
   * Setup responsive sidenav behavior
   */
  private setupResponsiveSidenav(): void {
    // Define breakpoints
    const mobileQuery = '(max-width: 767px)';
    const tabletQuery = '(min-width: 768px) and (max-width: 1023px)';
    const desktopQuery = '(min-width: 1024px)';
    
    // Observe breakpoints
    this.breakpointObserver
      .observe([mobileQuery, tabletQuery, desktopQuery])
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        this.isMobile = result.breakpoints[mobileQuery];
        this.isTablet = result.breakpoints[tabletQuery];
        this.isDesktop = result.breakpoints[desktopQuery];
        
        // Update sidenav behavior based on breakpoint
        if (this.isMobile) {
          this.sidenavMode$.next('over');
          this.sidenavOpened$.next(false);
          this.isCollapsed = false; // Don't collapse on mobile
        } else if (this.isTablet) {
          this.sidenavMode$.next('over');
          this.sidenavOpened$.next(false);
          this.isCollapsed = false;
        } else {
          this.sidenavMode$.next('side');
          this.sidenavOpened$.next(true);
          // Keep collapsed state on desktop
        }
        
        this.cdr.markForCheck();
      });
  }
  
  /**
   * Setup router events for navigation handling
   */
  private setupRouterEvents(): void {
    this.router.events
      .pipe(
        takeUntil(this.destroy$),
        filter(event => event instanceof NavigationEnd)
      )
      .subscribe(() => {
        // Auto-close sidenav on mobile after navigation
        if (this.isMobile && this.sidenavOpened$.value) {
          this.sidenavOpened$.next(false);
        }
      });
  }
  
  /**
   * Toggle sidenav open/closed or collapsed state
   */
  toggleSidenav(): void {
    if (this.isDesktop && this.sidenavOpened$.value) {
      // On desktop, toggle between expanded and collapsed
      this.isCollapsed = !this.isCollapsed;
    } else {
      // On mobile/tablet or when closed, toggle open/closed
      this.sidenavOpened$.next(!this.sidenavOpened$.value);
      if (this.sidenavOpened$.value) {
        this.isCollapsed = false;
      }
    }
  }
  
  /**
   * Handle navigation item click
   */
  onNavItemClick(): void {
    // Close sidenav on mobile after navigation
    if (this.isMobile || this.isTablet) {
      this.sidenavOpened$.next(false);
    }
  }
  
  /**
   * Toggle theme between light and dark
   */
  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
  
  /**
   * Navigate to all notifications page
   */
  viewAllNotifications(): void {
    this.router.navigate(['/notifications']);
  }
  
  /**
   * Logout user
   */
  logout(): void {
    // TODO: Implement logout logic
    console.log('Logout clicked');
    // This would typically:
    // 1. Clear auth tokens
    // 2. Disconnect WebSocket
    // 3. Navigate to login page
    this.webSocketService.disconnect();
    this.router.navigate(['/login']);
  }
}
