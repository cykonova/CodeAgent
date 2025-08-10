import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ThemeService } from '@core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule],
  template: `
    <button 
      mat-icon-button 
      [matTooltip]="tooltipText()"
      (click)="toggleTheme()"
      [attr.aria-label]="ariaLabel()">
      <mat-icon>{{ icon() }}</mat-icon>
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
  
  // Computed signals for reactive updates
  protected readonly icon = computed(() => 
    this.themeService.theme() === 'dark' ? 'light_mode' : 'dark_mode'
  );
  
  protected readonly tooltipText = computed(() => 
    this.themeService.theme() === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'
  );
  
  protected readonly ariaLabel = computed(() => 
    this.themeService.theme() === 'dark' ? 'Activate light theme' : 'Activate dark theme'
  );
  
  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}