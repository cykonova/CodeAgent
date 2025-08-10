import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService, User } from '@code-agent/auth';
import { HeaderService } from '@code-agent/data-access';
import { Observable } from 'rxjs';

@Component({
  imports: [
    RouterModule,
    CommonModule,
    MatToolbarModule,
    MatSidenavModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatMenuModule,
    MatTooltipModule,
    MatDividerModule
  ],
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private headerService = inject(HeaderService);
  
  protected title = 'Code Agent';
  protected sidenavOpened = true;
  protected isDarkTheme = false;
  protected isAuthenticated$!: Observable<boolean>;
  protected currentUser$!: Observable<User | null>;
  
  ngOnInit(): void {
    this.isAuthenticated$ = this.authService.isAuthenticated$;
    this.currentUser$ = this.authService.currentUser$;
    
    this.headerService.pageTitle$.subscribe(pageTitle => {
      if (pageTitle) {
        this.title = `Code Agent - ${pageTitle}`;
      } else {
        this.title = 'Code Agent';
      }
    });
  }

  toggleSidenav(): void {
    this.sidenavOpened = !this.sidenavOpened;
  }

  toggleTheme(): void {
    this.isDarkTheme = !this.isDarkTheme;
    document.body.classList.toggle('dark-theme', this.isDarkTheme);
  }
  
  logout(): void {
    this.authService.logout();
  }
  
  navigateToProfile(): void {
    this.router.navigate(['/profile']);
  }
}
