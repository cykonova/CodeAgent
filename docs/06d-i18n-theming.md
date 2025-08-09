# Phase 6d: Internationalization & Theming

## Overview
Implement comprehensive internationalization (i18n) support and advanced theming capabilities, including RTL support, dynamic theme switching, and locale-specific formatting.

## Visual References
The mockups demonstrate theming and i18n features:
- **Theme Switching**: Light/dark themes defined in [`shell/main-layout.mml`](../mockups/shell/main-layout.mml)
- **Language Selection**: Language dropdown shown in [`settings/general-settings.mml`](../mockups/settings/general-settings.mml)
- **Locale Formatting**: Date/time formats throughout all mockups
- **RTL Support**: Prepared for Arabic/Hebrew layouts in navigation and forms

## Objectives
- Setup Angular i18n with multiple languages
- Implement dynamic theme switching (light/dark/custom)
- Add RTL support for Arabic and Hebrew
- Create locale-aware formatting utilities
- Build translation management system

## Internationalization Setup

### 1. Angular i18n Configuration

#### Install i18n Dependencies
```bash
npm install @angular/localize
ng add @angular/localize

# Install additional locale data
npm install @angular/common
```

#### Configure i18n in Angular
```json
// angular.json
{
  "projects": {
    "shell": {
      "i18n": {
        "sourceLocale": "en-US",
        "locales": {
          "es": "libs/i18n/locales/es-ES.json",
          "fr": "libs/i18n/locales/fr-FR.json",
          "de": "libs/i18n/locales/de-DE.json",
          "ja": "libs/i18n/locales/ja-JP.json",
          "ar": "libs/i18n/locales/ar-SA.json",
          "he": "libs/i18n/locales/he-IL.json",
          "zh": "libs/i18n/locales/zh-CN.json"
        }
      },
      "architect": {
        "build": {
          "options": {
            "localize": true,
            "i18nMissingTranslation": "warning"
          },
          "configurations": {
            "production": {
              "localize": ["en-US", "es", "fr", "de", "ja", "ar", "he", "zh"]
            }
          }
        }
      }
    }
  }
}
```

### 2. Translation Service

#### Generate i18n Library
```bash
nx g @nx/angular:library i18n --directory=libs/i18n --standalone
```

#### Translation Service Implementation
```typescript
// libs/i18n/src/lib/services/translation.service.ts
@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private currentLocale$ = new BehaviorSubject<string>('en-US');
  private translations$ = new BehaviorSubject<Record<string, any>>({});
  private dateAdapter: DateAdapter<any>;
  
  readonly locale$ = this.currentLocale$.asObservable();
  readonly isRtl$ = this.currentLocale$.pipe(
    map(locale => ['ar-SA', 'he-IL'].includes(locale))
  );
  
  constructor(
    @Inject(LOCALE_ID) private localeId: string,
    private http: HttpClient,
    dateAdapter: DateAdapter<any>
  ) {
    this.dateAdapter = dateAdapter;
    this.setLocale(this.localeId);
  }
  
  setLocale(locale: string): void {
    this.currentLocale$.next(locale);
    this.loadTranslations(locale);
    this.dateAdapter.setLocale(locale);
    
    // Update HTML direction for RTL languages
    const isRtl = ['ar-SA', 'he-IL'].includes(locale);
    document.dir = isRtl ? 'rtl' : 'ltr';
    document.documentElement.lang = locale;
  }
  
  private loadTranslations(locale: string): void {
    this.http.get(`/assets/i18n/${locale}.json`)
      .subscribe(translations => {
        this.translations$.next(translations as Record<string, any>);
      });
  }
  
  translate(key: string, params?: Record<string, any>): string {
    const translations = this.translations$.value;
    let translation = this.getNestedProperty(translations, key) || key;
    
    if (params) {
      Object.keys(params).forEach(param => {
        translation = translation.replace(`{{${param}}}`, params[param]);
      });
    }
    
    return translation;
  }
  
  private getNestedProperty(obj: any, path: string): any {
    return path.split('.').reduce((curr, prop) => curr?.[prop], obj);
  }
  
  // Pluralization support
  translatePlural(key: string, count: number, params?: Record<string, any>): string {
    const pluralKey = this.getPluralKey(key, count);
    return this.translate(pluralKey, { ...params, count });
  }
  
  private getPluralKey(key: string, count: number): string {
    const locale = this.currentLocale$.value;
    const pluralRules = new Intl.PluralRules(locale);
    const rule = pluralRules.select(count);
    
    return `${key}.${rule}`;
  }
}
```

### 3. Translation Files Structure

#### English (en-US.json)
```json
{
  "common": {
    "save": "Save",
    "cancel": "Cancel",
    "delete": "Delete",
    "edit": "Edit",
    "search": "Search",
    "loading": "Loading...",
    "error": "Error",
    "success": "Success",
    "confirm": "Confirm"
  },
  "navigation": {
    "dashboard": "Dashboard",
    "projects": "Projects",
    "chat": "Chat",
    "settings": "Settings"
  },
  "dashboard": {
    "title": "System Overview",
    "activeAgents": "Active Agents",
    "apiCalls": "API Calls",
    "avgResponseTime": "Avg Response Time",
    "providers": "Providers",
    "last24Hours": "Last 24 hours"
  },
  "projects": {
    "title": "Projects",
    "newProject": "New Project",
    "projectName": "Project Name",
    "status": "Status",
    "lastModified": "Last Modified",
    "agents": "Agents",
    "noProjects": "No projects found",
    "createFirst": "Create your first project to get started"
  },
  "chat": {
    "title": "Agent Chat",
    "selectAgent": "Select an agent",
    "typeMessage": "Type your message",
    "send": "Send",
    "agentTyping": "{{agent}} is typing...",
    "noAgents": "No active agents"
  },
  "settings": {
    "title": "Settings",
    "general": "General",
    "providers": "Providers",
    "security": "Security",
    "theme": "Theme",
    "language": "Language",
    "notifications": "Enable notifications",
    "apiUrl": "API URL",
    "sandboxLevel": "Sandbox Level",
    "requireAuth": "Require authentication",
    "enableLogging": "Enable audit logging"
  },
  "validation": {
    "required": "{{field}} is required",
    "email": "Invalid email format",
    "minLength": "Minimum length is {{min}}",
    "maxLength": "Maximum length is {{max}}",
    "pattern": "Invalid format",
    "url": "Invalid URL format",
    "json": "Invalid JSON format"
  },
  "messages": {
    "saveSuccess": "Changes saved successfully",
    "deleteConfirm": "Are you sure you want to delete {{item}}?",
    "connectionTest": {
      "success": "Connection successful",
      "failure": "Connection failed"
    },
    "items": {
      "zero": "No items",
      "one": "1 item",
      "other": "{{count}} items"
    }
  }
}
```

#### Arabic (ar-SA.json) - RTL
```json
{
  "common": {
    "save": "حفظ",
    "cancel": "إلغاء",
    "delete": "حذف",
    "edit": "تعديل",
    "search": "بحث",
    "loading": "جار التحميل...",
    "error": "خطأ",
    "success": "نجاح",
    "confirm": "تأكيد"
  },
  "navigation": {
    "dashboard": "لوحة القيادة",
    "projects": "المشاريع",
    "chat": "محادثة",
    "settings": "الإعدادات"
  }
}
```

### 4. Translation Pipe

```typescript
// libs/i18n/src/lib/pipes/translate.pipe.ts
@Pipe({
  name: 'translate',
  standalone: true,
  pure: false
})
export class TranslatePipe implements PipeTransform {
  constructor(private translationService: TranslationService) {}
  
  transform(key: string, params?: Record<string, any>): string {
    return this.translationService.translate(key, params);
  }
}

// libs/i18n/src/lib/pipes/translate-plural.pipe.ts
@Pipe({
  name: 'translatePlural',
  standalone: true,
  pure: false
})
export class TranslatePluralPipe implements PipeTransform {
  constructor(private translationService: TranslationService) {}
  
  transform(key: string, count: number, params?: Record<string, any>): string {
    return this.translationService.translatePlural(key, count, params);
  }
}
```

## Advanced Theming System

### 1. Theme Service

```typescript
// libs/ui/theme/src/lib/theme.service.ts
@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private currentTheme$ = new BehaviorSubject<ThemeConfig>('light');
  private customThemes = new Map<string, ThemeConfig>();
  
  readonly theme$ = this.currentTheme$.asObservable();
  
  constructor(
    @Inject(DOCUMENT) private document: Document,
    private renderer: Renderer2
  ) {
    this.initializeTheme();
  }
  
  private initializeTheme(): void {
    const savedTheme = localStorage.getItem('theme') || 'light';
    this.setTheme(savedTheme as ThemeType);
    
    // Listen for system theme changes
    if (window.matchMedia) {
      const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');
      darkModeQuery.addEventListener('change', (e) => {
        if (this.currentTheme$.value === 'auto') {
          this.applySystemTheme();
        }
      });
    }
  }
  
  setTheme(theme: ThemeType): void {
    localStorage.setItem('theme', theme);
    
    if (theme === 'auto') {
      this.applySystemTheme();
    } else {
      this.applyTheme(theme);
    }
    
    this.currentTheme$.next(theme);
  }
  
  private applySystemTheme(): void {
    const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    this.applyTheme(isDark ? 'dark' : 'light');
  }
  
  private applyTheme(theme: string): void {
    const body = this.document.body;
    
    // Remove existing theme classes
    body.classList.remove('light-theme', 'dark-theme', 'high-contrast-theme');
    
    // Add new theme class
    body.classList.add(`${theme}-theme`);
    
    // Apply custom theme if exists
    if (this.customThemes.has(theme)) {
      this.applyCustomTheme(this.customThemes.get(theme)!);
    }
  }
  
  registerCustomTheme(name: string, config: ThemeConfig): void {
    this.customThemes.set(name, config);
  }
  
  private applyCustomTheme(config: ThemeConfig): void {
    const root = this.document.documentElement;
    
    Object.entries(config.colors).forEach(([key, value]) => {
      root.style.setProperty(`--theme-${key}`, value);
    });
    
    if (config.typography) {
      Object.entries(config.typography).forEach(([key, value]) => {
        root.style.setProperty(`--typography-${key}`, value);
      });
    }
  }
}

export type ThemeType = 'light' | 'dark' | 'auto' | string;

export interface ThemeConfig {
  colors: {
    primary: string;
    accent: string;
    warn: string;
    background: string;
    surface: string;
    text: string;
    [key: string]: string;
  };
  typography?: {
    fontFamily: string;
    fontSize: string;
    [key: string]: string;
  };
}
```

### 2. Theme Styles

```scss
// libs/ui/theme/src/lib/styles/_themes.scss
@use '@angular/material' as mat;
@use 'sass:map';

// Light Theme
$light-primary: mat.define-palette(mat.$indigo-palette);
$light-accent: mat.define-palette(mat.$pink-palette, A200, A100, A400);
$light-warn: mat.define-palette(mat.$red-palette);

$light-theme: mat.define-light-theme((
  color: (
    primary: $light-primary,
    accent: $light-accent,
    warn: $light-warn,
  ),
  typography: mat.define-typography-config(
    $font-family: 'Roboto, "Helvetica Neue", sans-serif',
  ),
  density: 0,
));

// Dark Theme
$dark-primary: mat.define-palette(mat.$cyan-palette);
$dark-accent: mat.define-palette(mat.$amber-palette, A200, A100, A400);
$dark-warn: mat.define-palette(mat.$deep-orange-palette);

$dark-theme: mat.define-dark-theme((
  color: (
    primary: $dark-primary,
    accent: $dark-accent,
    warn: $dark-warn,
  ),
  typography: mat.define-typography-config(
    $font-family: 'Roboto, "Helvetica Neue", sans-serif',
  ),
  density: 0,
));

// High Contrast Theme
$hc-primary: mat.define-palette(mat.$yellow-palette);
$hc-accent: mat.define-palette(mat.$lime-palette);
$hc-warn: mat.define-palette(mat.$orange-palette);

$high-contrast-theme: mat.define-dark-theme((
  color: (
    primary: $hc-primary,
    accent: $hc-accent,
    warn: $hc-warn,
  ),
  typography: mat.define-typography-config(
    $font-family: 'Roboto, "Helvetica Neue", sans-serif',
    $headline-1: mat.define-typography-level(96px, 96px, 500),
  ),
  density: -1,
));

// Apply themes
.light-theme {
  @include mat.all-component-themes($light-theme);
  
  // Custom properties
  --bg-primary: #ffffff;
  --bg-secondary: #f5f5f5;
  --text-primary: rgba(0, 0, 0, 0.87);
  --text-secondary: rgba(0, 0, 0, 0.60);
  --border-color: rgba(0, 0, 0, 0.12);
}

.dark-theme {
  @include mat.all-component-themes($dark-theme);
  
  // Custom properties
  --bg-primary: #1e1e1e;
  --bg-secondary: #2d2d2d;
  --text-primary: rgba(255, 255, 255, 0.87);
  --text-secondary: rgba(255, 255, 255, 0.60);
  --border-color: rgba(255, 255, 255, 0.12);
}

.high-contrast-theme {
  @include mat.all-component-themes($high-contrast-theme);
  
  // Custom properties
  --bg-primary: #000000;
  --bg-secondary: #1a1a1a;
  --text-primary: #ffffff;
  --text-secondary: #ffff00;
  --border-color: #ffffff;
}

// RTL Support
[dir="rtl"] {
  .mat-drawer {
    transform: translateX(100%);
  }
  
  .mat-drawer.mat-drawer-opened {
    transform: translateX(0);
  }
  
  .mat-form-field-prefix {
    margin-left: 0.5em;
    margin-right: 0;
  }
  
  .mat-form-field-suffix {
    margin-right: 0.5em;
    margin-left: 0;
  }
  
  // Flip icons that need direction change
  .mat-icon.flip-rtl {
    transform: scaleX(-1);
  }
}
```

### 3. Locale-Aware Formatting

```typescript
// libs/i18n/src/lib/services/locale-format.service.ts
@Injectable({
  providedIn: 'root'
})
export class LocaleFormatService {
  constructor(
    @Inject(LOCALE_ID) private localeId: string,
    private translationService: TranslationService
  ) {}
  
  formatDate(date: Date | string, format: 'short' | 'medium' | 'long' | 'full' = 'medium'): string {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return formatDate(dateObj, format, this.localeId);
  }
  
  formatNumber(value: number, digitsInfo?: string): string {
    return formatNumber(value, this.localeId, digitsInfo);
  }
  
  formatCurrency(value: number, currencyCode?: string, display?: 'code' | 'symbol' | 'symbol-narrow'): string {
    const currency = currencyCode || this.getLocaleCurrency();
    return formatCurrency(value, this.localeId, currency, display || 'symbol');
  }
  
  formatPercent(value: number, digitsInfo?: string): string {
    return formatPercent(value, this.localeId, digitsInfo);
  }
  
  formatFileSize(bytes: number): string {
    const units = this.translationService.translate('units.fileSize').split(',');
    if (bytes === 0) return `0 ${units[0]}`;
    
    const k = 1024;
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    const size = (bytes / Math.pow(k, i)).toFixed(2);
    
    return `${this.formatNumber(parseFloat(size))} ${units[i]}`;
  }
  
  formatDuration(milliseconds: number): string {
    const seconds = Math.floor(milliseconds / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    
    if (days > 0) {
      return this.translationService.translate('units.duration.days', { count: days });
    } else if (hours > 0) {
      return this.translationService.translate('units.duration.hours', { count: hours });
    } else if (minutes > 0) {
      return this.translationService.translate('units.duration.minutes', { count: minutes });
    } else {
      return this.translationService.translate('units.duration.seconds', { count: seconds });
    }
  }
  
  formatRelativeTime(date: Date | string): string {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    const now = new Date();
    const diff = now.getTime() - dateObj.getTime();
    
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    
    if (days > 7) {
      return this.formatDate(dateObj, 'short');
    } else if (days > 0) {
      return this.translationService.translatePlural('time.daysAgo', days);
    } else if (hours > 0) {
      return this.translationService.translatePlural('time.hoursAgo', hours);
    } else if (minutes > 0) {
      return this.translationService.translatePlural('time.minutesAgo', minutes);
    } else {
      return this.translationService.translate('time.justNow');
    }
  }
  
  private getLocaleCurrency(): string {
    const currencyMap: Record<string, string> = {
      'en-US': 'USD',
      'es-ES': 'EUR',
      'fr-FR': 'EUR',
      'de-DE': 'EUR',
      'ja-JP': 'JPY',
      'zh-CN': 'CNY',
      'ar-SA': 'SAR',
      'he-IL': 'ILS'
    };
    
    return currencyMap[this.localeId] || 'USD';
  }
}
```

### 4. Language Selector Component

```typescript
// libs/ui/i18n/src/lib/language-selector/language-selector.component.ts
@Component({
  selector: 'ui-language-selector',
  standalone: true,
  imports: [
    CommonModule,
    MatSelectModule,
    MatFormFieldModule,
    MatIconModule
  ],
  template: `
    <mat-form-field appearance="outline" [class.rtl]="isRtl$ | async">
      <mat-label>{{ 'settings.language' | translate }}</mat-label>
      <mat-select [value]="currentLocale$ | async" (valueChange)="onLocaleChange($event)">
        <mat-option *ngFor="let lang of languages" [value]="lang.code">
          <span class="flag">{{ lang.flag }}</span>
          {{ lang.name }}
          <span class="native-name">{{ lang.nativeName }}</span>
        </mat-option>
      </mat-select>
      <mat-icon matPrefix>language</mat-icon>
    </mat-form-field>
  `,
  styles: [`
    .flag {
      margin-right: 8px;
      font-size: 1.2em;
    }
    .native-name {
      margin-left: 8px;
      opacity: 0.7;
      font-size: 0.9em;
    }
    .rtl {
      direction: rtl;
      
      .flag {
        margin-right: 0;
        margin-left: 8px;
      }
      
      .native-name {
        margin-left: 0;
        margin-right: 8px;
      }
    }
  `]
})
export class LanguageSelectorComponent {
  languages = [
    { code: 'en-US', name: 'English', nativeName: 'English', flag: '🇺🇸' },
    { code: 'es-ES', name: 'Spanish', nativeName: 'Español', flag: '🇪🇸' },
    { code: 'fr-FR', name: 'French', nativeName: 'Français', flag: '🇫🇷' },
    { code: 'de-DE', name: 'German', nativeName: 'Deutsch', flag: '🇩🇪' },
    { code: 'ja-JP', name: 'Japanese', nativeName: '日本語', flag: '🇯🇵' },
    { code: 'zh-CN', name: 'Chinese', nativeName: '中文', flag: '🇨🇳' },
    { code: 'ar-SA', name: 'Arabic', nativeName: 'العربية', flag: '🇸🇦' },
    { code: 'he-IL', name: 'Hebrew', nativeName: 'עברית', flag: '🇮🇱' }
  ];
  
  currentLocale$ = this.translationService.locale$;
  isRtl$ = this.translationService.isRtl$;
  
  constructor(private translationService: TranslationService) {}
  
  onLocaleChange(locale: string): void {
    this.translationService.setLocale(locale);
    
    // Reload the application with new locale
    window.location.href = `/${locale}/`;
  }
}
```

## Success Criteria
- [ ] Angular i18n configured with 8+ languages
- [ ] Translation service with pluralization support
- [ ] RTL support for Arabic and Hebrew
- [ ] Dynamic theme switching (light/dark/custom)
- [ ] Locale-aware formatting for dates, numbers, currency
- [ ] Language selector component
- [ ] All UI text externalized to translation files
- [ ] Theme persistence in localStorage
- [ ] System theme detection for auto mode
- [ ] Accessibility maintained across all themes

## Next Steps
After completing this phase:
1. Update main Phase 6 document as overview
2. Test all language translations
3. Verify RTL layout in Arabic/Hebrew
4. Ensure theme switching works across all remotes
5. Run accessibility audit for all themes