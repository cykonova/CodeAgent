import { Injectable, signal } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type SupportedLocale = 'en-US' | 'es-ES' | 'fr-FR' | 'de-DE' | 'ja-JP' | 'zh-CN' | 'ar-SA' | 'he-IL' | 'pt-BR';

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private currentLocale = signal<SupportedLocale>('en-US');
  private translations = new Map<SupportedLocale, any>();
  private translationsSubject = new BehaviorSubject<any>({});
  
  public translations$ = this.translationsSubject.asObservable();

  constructor() {
    this.loadTranslations();
  }

  private async loadTranslations(): Promise<void> {
    const locales: SupportedLocale[] = [
      'en-US', 'es-ES', 'fr-FR', 'de-DE', 
      'ja-JP', 'zh-CN', 'ar-SA', 'he-IL', 'pt-BR'
    ];
    
    for (const locale of locales) {
      try {
        const translations = await import(`./locales/${locale}.json`);
        this.translations.set(locale, translations.default);
      } catch (error) {
        console.error(`Failed to load translations for ${locale}:`, error);
      }
    }
    
    this.setLocale(this.getDefaultLocale());
  }

  private getDefaultLocale(): SupportedLocale {
    const browserLang = navigator.language;
    const supportedLocales: SupportedLocale[] = [
      'en-US', 'es-ES', 'fr-FR', 'de-DE', 
      'ja-JP', 'zh-CN', 'ar-SA', 'he-IL', 'pt-BR'
    ];
    
    const matchedLocale = supportedLocales.find(locale => 
      browserLang.startsWith(locale.split('-')[0])
    );
    
    return matchedLocale || 'en-US';
  }

  setLocale(locale: SupportedLocale): void {
    this.currentLocale.set(locale);
    const translations = this.translations.get(locale) || this.translations.get('en-US');
    this.translationsSubject.next(translations);
    
    // Set document direction for RTL languages
    if (locale === 'ar-SA' || locale === 'he-IL') {
      document.dir = 'rtl';
      document.documentElement.lang = locale;
      document.body.classList.add('rtl');
    } else {
      document.dir = 'ltr';
      document.documentElement.lang = locale;
      document.body.classList.remove('rtl');
    }
  }

  getLocale(): SupportedLocale {
    return this.currentLocale();
  }

  translate(key: string): string {
    const translations = this.translationsSubject.value;
    const keys = key.split('.');
    let value = translations;
    
    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k];
      } else {
        return key; // Return key if translation not found
      }
    }
    
    return value;
  }

  instant(key: string): string {
    return this.translate(key);
  }
}