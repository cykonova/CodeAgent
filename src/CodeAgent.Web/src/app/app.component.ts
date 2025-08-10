import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
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
import { Subject } from 'rxjs';
import { takeUntil, filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ThemeToggleComponent,
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
  
  // Sidenav state
  sidenavOpened = true;
  sidenavMode: 'side' | 'over' = 'side';
  
  // Responsive state
  isMobile = false;
  isTablet = false;
  isDesktop = true;
  
  // Loading state
  isLoading = false;
  
  private destroy$ = new Subject<void>();
  
  constructor(
    private breakpointObserver: BreakpointObserver,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}
  
  ngOnInit(): void {
    this.setupResponsiveLayout();
    this.setupRouterEvents();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  private setupResponsiveLayout(): void {
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
        if (this.isMobile || this.isTablet) {
          this.sidenavMode = 'over';
          this.sidenavOpened = false;
        } else {
          this.sidenavMode = 'side';
          this.sidenavOpened = true;
        }
        
        this.cdr.markForCheck();
      });
  }
  
  private setupRouterEvents(): void {
    // Handle loading states during navigation
    this.router.events
      .pipe(
        takeUntil(this.destroy$),
        filter(event => 
          event instanceof NavigationStart || 
          event instanceof NavigationEnd || 
          event instanceof NavigationCancel || 
          event instanceof NavigationError
        )
      )
      .subscribe(event => {
        if (event instanceof NavigationStart) {
          this.isLoading = true;
        } else {
          this.isLoading = false;
          
          // Auto-close sidenav on mobile after navigation
          if (this.isMobile && this.sidenavOpened) {
            this.sidenavOpened = false;
          }
        }
        
        this.cdr.markForCheck();
      });
  }
  
  toggleSidenav(): void {
    this.sidenavOpened = !this.sidenavOpened;
  }
  
  onSidenavClick(): void {
    // Close sidenav on mobile when clicking a link
    if (this.isMobile || this.isTablet) {
      this.sidenavOpened = false;
    }
  }
}
