import { Injectable, signal, effect } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { inject } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'theme-preference';
  
  // Use Angular signals for reactive theme state
  private themeSignal = signal<Theme>(this.getStoredTheme());
  
  // Public readonly signal for components to subscribe to
  public readonly theme = this.themeSignal.asReadonly();
  
  constructor() {
    // Apply theme on initialization
    this.applyTheme(this.themeSignal());
    
    // Listen for system theme changes
    this.setupSystemThemeListener();
    
    // Use effect to persist theme changes
    effect(() => {
      const theme = this.themeSignal();
      this.applyTheme(theme);
      this.storeTheme(theme);
    });
  }
  
  /**
   * Toggle between light and dark themes
   */
  toggleTheme(): void {
    const currentTheme = this.themeSignal();
    const newTheme: Theme = currentTheme === 'light' ? 'dark' : 'light';
    this.setTheme(newTheme);
  }
  
  /**
   * Set specific theme
   */
  setTheme(theme: Theme): void {
    // Add transition disable class temporarily
    this.document.body.classList.add('theme-transition-none');
    
    // Update theme signal
    this.themeSignal.set(theme);
    
    // Re-enable transitions after a brief delay
    setTimeout(() => {
      this.document.body.classList.remove('theme-transition-none');
    }, 50);
  }
  
  /**
   * Get current theme
   */
  getCurrentTheme(): Theme {
    return this.themeSignal();
  }
  
  /**
   * Check if dark mode is active
   */
  isDarkMode(): boolean {
    return this.themeSignal() === 'dark';
  }
  
  /**
   * Apply theme to document
   */
  private applyTheme(theme: Theme): void {
    const body = this.document.body;
    
    if (theme === 'dark') {
      body.classList.add('dark-theme');
      body.classList.remove('light-theme');
    } else {
      body.classList.remove('dark-theme');
      body.classList.add('light-theme');
    }
    
    // Update meta theme-color for mobile browsers
    this.updateMetaThemeColor(theme);
  }
  
  /**
   * Update meta theme-color tag
   */
  private updateMetaThemeColor(theme: Theme): void {
    let metaThemeColor = this.document.querySelector('meta[name="theme-color"]');
    
    if (!metaThemeColor) {
      metaThemeColor = this.document.createElement('meta');
      metaThemeColor.setAttribute('name', 'theme-color');
      this.document.head.appendChild(metaThemeColor);
    }
    
    // Set appropriate color based on theme
    const color = theme === 'dark' ? '#1e1e1e' : '#ffffff';
    metaThemeColor.setAttribute('content', color);
  }
  
  /**
   * Get stored theme preference
   */
  private getStoredTheme(): Theme {
    // Check localStorage first
    const storedTheme = localStorage.getItem(this.storageKey) as Theme;
    if (storedTheme === 'light' || storedTheme === 'dark') {
      return storedTheme;
    }
    
    // Check system preference
    return this.getSystemTheme();
  }
  
  /**
   * Store theme preference
   */
  private storeTheme(theme: Theme): void {
    localStorage.setItem(this.storageKey, theme);
  }
  
  /**
   * Get system theme preference
   */
  private getSystemTheme(): Theme {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
  
  /**
   * Setup listener for system theme changes
   */
  private setupSystemThemeListener(): void {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    
    // Only auto-switch if user hasn't set a preference
    mediaQuery.addEventListener('change', (e) => {
      if (!localStorage.getItem(this.storageKey)) {
        const theme = e.matches ? 'dark' : 'light';
        this.setTheme(theme);
      }
    });
  }
  
  /**
   * Clear stored theme preference and use system default
   */
  clearPreference(): void {
    localStorage.removeItem(this.storageKey);
    this.setTheme(this.getSystemTheme());
  }
}