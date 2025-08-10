import { Injectable, Inject, Renderer2, RendererFactory2, OnDestroy } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { BehaviorSubject, Observable, Subject, fromEvent } from 'rxjs';
import { takeUntil, map } from 'rxjs/operators';
import { ThemeMode, ThemeConfig, ThemeColors, ThemeChangeEvent } from '../models/theme.models';

@Injectable({
  providedIn: 'root'
})
export class ThemeService implements OnDestroy {
  private readonly STORAGE_KEY = 'app-theme-preference';
  private readonly THEME_CLASS_PREFIX = 'theme-';
  
  private currentThemeSubject = new BehaviorSubject<ThemeMode>(ThemeMode.System);
  private isDarkModeSubject = new BehaviorSubject<boolean>(false);
  private themeColorsSubject = new BehaviorSubject<ThemeColors>(this.getLightColors());
  private themeChangeSubject = new Subject<ThemeChangeEvent>();
  
  public currentTheme$ = this.currentThemeSubject.asObservable();
  public isDarkMode$ = this.isDarkModeSubject.asObservable();
  public themeColors$ = this.themeColorsSubject.asObservable();
  public themeChange$ = this.themeChangeSubject.asObservable();
  
  private mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  private destroy$ = new Subject<void>();
  private renderer: Renderer2;
  
  constructor(
    @Inject(DOCUMENT) private document: Document,
    rendererFactory: RendererFactory2
  ) {
    this.renderer = rendererFactory.createRenderer(null, null);
    this.initializeTheme();
    this.setupSystemThemeListener();
  }
  
  private initializeTheme(): void {
    // Load saved preference
    const savedTheme = this.loadThemePreference();
    
    // Apply initial theme
    this.setTheme(savedTheme || ThemeMode.System);
    
    // Set up CSS custom properties
    this.updateCssVariables();
  }
  
  private loadThemePreference(): ThemeMode | null {
    const saved = localStorage.getItem(this.STORAGE_KEY);
    return saved as ThemeMode || null;
  }
  
  private saveThemePreference(theme: ThemeMode): void {
    localStorage.setItem(this.STORAGE_KEY, theme);
  }
  
  public setTheme(mode: ThemeMode): void {
    this.currentThemeSubject.next(mode);
    this.saveThemePreference(mode);
    
    const isDark = this.shouldUseDarkTheme(mode);
    this.applyTheme(isDark);
  }
  
  public toggleTheme(): void {
    const currentMode = this.currentThemeSubject.value;
    
    if (currentMode === ThemeMode.System) {
      // If system, switch to opposite of current
      const newMode = this.isDarkModeSubject.value ? ThemeMode.Light : ThemeMode.Dark;
      this.setTheme(newMode);
    } else {
      // Toggle between light and dark
      const newMode = currentMode === ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
      this.setTheme(newMode);
    }
  }
  
  private shouldUseDarkTheme(mode: ThemeMode): boolean {
    switch (mode) {
      case ThemeMode.Dark:
        return true;
      case ThemeMode.Light:
        return false;
      case ThemeMode.System:
        return this.mediaQuery.matches;
      default:
        return false;
    }
  }
  
  private applyTheme(isDark: boolean): void {
    const previousDarkMode = this.isDarkModeSubject.value;
    
    // Update state
    this.isDarkModeSubject.next(isDark);
    
    // Update document class
    this.updateDocumentClass(isDark);
    
    // Update theme colors
    const colors = isDark ? this.getDarkColors() : this.getLightColors();
    this.themeColorsSubject.next(colors);
    
    // Update CSS variables
    this.updateCssVariables(isDark);
    
    // Emit theme change event
    if (previousDarkMode !== isDark) {
      this.emitThemeChangeEvent(isDark);
    }
  }
  
  private updateDocumentClass(isDark: boolean): void {
    const body = this.document.body;
    
    // Remove existing theme classes
    const existingClasses = Array.from(body.classList).filter(
      className => className.startsWith(this.THEME_CLASS_PREFIX)
    );
    existingClasses.forEach(className => {
      this.renderer.removeClass(body, className);
    });
    
    // Add new theme class
    const themeClass = `${this.THEME_CLASS_PREFIX}${isDark ? 'dark' : 'light'}`;
    this.renderer.addClass(body, themeClass);
    
    // Add Material theme class
    if (isDark) {
      this.renderer.addClass(body, 'dark-theme');
      this.renderer.removeClass(body, 'light-theme');
    } else {
      this.renderer.removeClass(body, 'dark-theme');
      this.renderer.addClass(body, 'light-theme');
    }
  }
  
  private updateCssVariables(isDark: boolean = false): void {
    const root = this.document.documentElement;
    const variables = isDark ? this.getDarkVariables() : this.getLightVariables();
    
    Object.entries(variables).forEach(([key, value]) => {
      root.style.setProperty(key, value);
    });
  }
  
  private getLightVariables(): Record<string, string> {
    return {
      // Surface colors
      '--theme-background': '#fafafa',
      '--theme-surface': '#ffffff',
      '--theme-surface-variant': '#f5f5f5',
      
      // Text colors with opacity
      '--theme-text-primary': 'rgba(0, 0, 0, 0.87)',
      '--theme-text-secondary': 'rgba(0, 0, 0, 0.60)',
      '--theme-text-disabled': 'rgba(0, 0, 0, 0.38)',
      '--theme-text-hint': 'rgba(0, 0, 0, 0.38)',
      
      // State colors
      '--theme-hover': 'rgba(0, 0, 0, 0.04)',
      '--theme-selected': 'rgba(0, 0, 0, 0.08)',
      '--theme-activated': 'rgba(0, 0, 0, 0.12)',
      '--theme-pressed': 'rgba(0, 0, 0, 0.16)',
      
      // Divider
      '--theme-divider': 'rgba(0, 0, 0, 0.12)',
      
      // Shadows (adjusted for light theme)
      '--theme-shadow-sm': '0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24)',
      '--theme-shadow-md': '0 3px 6px rgba(0,0,0,0.15), 0 2px 4px rgba(0,0,0,0.12)',
      '--theme-shadow-lg': '0 10px 20px rgba(0,0,0,0.15), 0 3px 6px rgba(0,0,0,0.10)',
      
      // Scrollbar
      '--theme-scrollbar-track': '#f1f1f1',
      '--theme-scrollbar-thumb': '#888',
      '--theme-scrollbar-thumb-hover': '#555',
      
      // Skeleton loader colors
      '--theme-skeleton-base': '#e0e0e0',
      '--theme-skeleton-shimmer': 'rgba(255, 255, 255, 0.4)',
      
      // Status colors
      '--theme-status-success': '#4caf50',
      '--theme-status-warning': '#ff9800',
      '--theme-status-error': '#f44336',
      '--theme-status-info': '#2196f3',
      '--theme-status-idle': '#9e9e9e',
      '--theme-status-running': '#4caf50',
      '--theme-status-paused': '#ff9800',
      
      // Loading overlay
      '--theme-overlay-backdrop': 'rgba(0, 0, 0, 0.5)',
      
      // Progress colors (uses Material theme colors)
      '--theme-progress-track': 'rgba(0, 0, 0, 0.12)',
      '--theme-progress-fill-primary': '#1976d2',
      '--theme-progress-fill-accent': '#ff9800',
      '--theme-progress-fill-warn': '#f44336'
    };
  }
  
  private getDarkVariables(): Record<string, string> {
    return {
      // Surface colors
      '--theme-background': '#303030',
      '--theme-surface': '#424242',
      '--theme-surface-variant': '#1e1e1e',
      
      // Text colors with opacity
      '--theme-text-primary': 'rgba(255, 255, 255, 1.00)',
      '--theme-text-secondary': 'rgba(255, 255, 255, 0.70)',
      '--theme-text-disabled': 'rgba(255, 255, 255, 0.50)',
      '--theme-text-hint': 'rgba(255, 255, 255, 0.50)',
      
      // State colors
      '--theme-hover': 'rgba(255, 255, 255, 0.08)',
      '--theme-selected': 'rgba(255, 255, 255, 0.16)',
      '--theme-activated': 'rgba(255, 255, 255, 0.24)',
      '--theme-pressed': 'rgba(255, 255, 255, 0.32)',
      
      // Divider
      '--theme-divider': 'rgba(255, 255, 255, 0.12)',
      
      // Shadows (adjusted for dark theme)
      '--theme-shadow-sm': '0 1px 3px rgba(0,0,0,0.24), 0 1px 2px rgba(0,0,0,0.48)',
      '--theme-shadow-md': '0 3px 6px rgba(0,0,0,0.30), 0 2px 4px rgba(0,0,0,0.24)',
      '--theme-shadow-lg': '0 10px 20px rgba(0,0,0,0.30), 0 3px 6px rgba(0,0,0,0.20)',
      
      // Scrollbar
      '--theme-scrollbar-track': '#2e2e2e',
      '--theme-scrollbar-thumb': '#6b6b6b',
      '--theme-scrollbar-thumb-hover': '#959595',
      
      // Skeleton loader colors
      '--theme-skeleton-base': '#424242',
      '--theme-skeleton-shimmer': 'rgba(255, 255, 255, 0.08)',
      
      // Status colors (slightly adjusted for dark theme)
      '--theme-status-success': '#66bb6a',
      '--theme-status-warning': '#ffa726',
      '--theme-status-error': '#ef5350',
      '--theme-status-info': '#42a5f5',
      '--theme-status-idle': '#bdbdbd',
      '--theme-status-running': '#66bb6a',
      '--theme-status-paused': '#ffa726',
      
      // Loading overlay
      '--theme-overlay-backdrop': 'rgba(0, 0, 0, 0.7)',
      
      // Progress colors (uses Material theme colors for dark)
      '--theme-progress-track': 'rgba(255, 255, 255, 0.12)',
      '--theme-progress-fill-primary': '#90caf9',
      '--theme-progress-fill-accent': '#ffb74d',
      '--theme-progress-fill-warn': '#ef5350'
    };
  }
  
  private setupSystemThemeListener(): void {
    // Listen for system theme changes
    fromEvent<MediaQueryListEvent>(this.mediaQuery, 'change')
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        if (this.currentThemeSubject.value === ThemeMode.System) {
          this.applyTheme(event.matches);
        }
      });
  }
  
  public getSystemTheme(): ThemeMode {
    return this.mediaQuery.matches ? ThemeMode.Dark : ThemeMode.Light;
  }
  
  public isSystemDarkMode(): boolean {
    return this.mediaQuery.matches;
  }
  
  private getLightColors(): ThemeColors {
    return {
      primary: '#1976d2',      // Material Blue 700
      accent: '#ff9800',       // Material Orange 500
      warn: '#f44336',         // Material Red 500
      background: '#fafafa',
      surface: '#ffffff',
      text: 'rgba(0, 0, 0, 0.87)',
      textSecondary: 'rgba(0, 0, 0, 0.60)',
      divider: 'rgba(0, 0, 0, 0.12)'
    };
  }
  
  private getDarkColors(): ThemeColors {
    return {
      primary: '#90caf9',      // Material Blue 200
      accent: '#ffb74d',       // Material Orange 300
      warn: '#ef5350',         // Material Red 400
      background: '#303030',
      surface: '#424242',
      text: 'rgba(255, 255, 255, 1.00)',
      textSecondary: 'rgba(255, 255, 255, 0.70)',
      divider: 'rgba(255, 255, 255, 0.12)'
    };
  }
  
  public getContrastColor(backgroundColor: string): string {
    // Simple contrast calculation
    const rgb = this.hexToRgb(backgroundColor);
    const brightness = (rgb.r * 299 + rgb.g * 587 + rgb.b * 114) / 1000;
    return brightness > 128 ? '#000000' : '#ffffff';
  }
  
  private hexToRgb(hex: string): { r: number; g: number; b: number } {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
      r: parseInt(result[1], 16),
      g: parseInt(result[2], 16),
      b: parseInt(result[3], 16)
    } : { r: 0, g: 0, b: 0 };
  }
  
  private emitThemeChangeEvent(isDark: boolean): void {
    const event: ThemeChangeEvent = {
      isDark,
      mode: this.currentThemeSubject.value,
      colors: this.themeColorsSubject.value,
      timestamp: new Date()
    };
    
    this.themeChangeSubject.next(event);
    
    // Dispatch custom DOM event for non-Angular listeners
    const customEvent = new CustomEvent('themechange', { detail: event });
    this.document.dispatchEvent(customEvent);
  }
  
  /**
   * Clear stored theme preference and use system default
   */
  public clearPreference(): void {
    localStorage.removeItem(this.STORAGE_KEY);
    this.setTheme(ThemeMode.System);
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}