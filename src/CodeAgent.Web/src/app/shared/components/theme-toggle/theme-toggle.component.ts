import { Component, inject } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ThemeService } from '@core/services/theme.service';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule, AsyncPipe, MatButtonModule, MatIconModule, MatTooltipModule],
  template: `
    <button 
      mat-icon-button 
      [matTooltip]="tooltipText$ | async"
      (click)="toggleTheme()"
      [attr.aria-label]="ariaLabel$ | async">
      <mat-icon>{{ icon$ | async }}</mat-icon>
    </button>
  `,
  styles: [`
    :host {
      display: inline-block;
    }
  `]
})
export class ThemeToggleComponent {
  private readonly themeService = inject(ThemeService);
  
  // Reactive observables for UI updates
  protected readonly icon$ = this.themeService.isDarkMode$.pipe(
    map(isDark => isDark ? 'light_mode' : 'dark_mode')
  );
  
  protected readonly tooltipText$ = this.themeService.isDarkMode$.pipe(
    map(isDark => isDark ? 'Switch to light theme' : 'Switch to dark theme')
  );
  
  protected readonly ariaLabel$ = this.themeService.isDarkMode$.pipe(
    map(isDark => isDark ? 'Activate light theme' : 'Activate dark theme')
  );
  
  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}