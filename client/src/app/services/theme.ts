import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly storageKey = 'codeagent-theme';
  private readonly theme = signal<Theme>(this.loadTheme());
  
  public readonly currentTheme = this.theme.asReadonly();
  
  constructor() {
    // Apply theme on initialization and whenever it changes
    effect(() => {
      this.applyTheme(this.theme());
    });
  }
  
  private loadTheme(): Theme {
    const stored = localStorage.getItem(this.storageKey) as Theme;
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
    
    // Default to system preference
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
  
  private applyTheme(theme: Theme): void {
    const html = document.documentElement;
    
    if (theme === 'dark') {
      html.classList.add('dark-theme');
    } else {
      html.classList.remove('dark-theme');
    }
    
    // Store preference
    localStorage.setItem(this.storageKey, theme);
  }
  
  toggleTheme(): void {
    this.theme.update(current => current === 'light' ? 'dark' : 'light');
  }
  
  setTheme(theme: Theme): void {
    this.theme.set(theme);
  }
  
  isDark(): boolean {
    return this.theme() === 'dark';
  }
}