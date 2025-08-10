export enum ThemeMode {
  Light = 'light',
  Dark = 'dark',
  System = 'system'
}

export interface ThemeConfig {
  mode: ThemeMode;
  primaryColor?: string;
  accentColor?: string;
  customProperties?: Record<string, string>;
}

export interface ThemeColors {
  primary: string;
  accent: string;
  warn: string;
  background: string;
  surface: string;
  text: string;
  textSecondary: string;
  divider: string;
}

export interface ThemeChangeEvent {
  isDark: boolean;
  mode: ThemeMode;
  colors: ThemeColors;
  timestamp: Date;
}