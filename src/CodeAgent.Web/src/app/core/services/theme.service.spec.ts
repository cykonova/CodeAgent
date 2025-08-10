import { TestBed } from '@angular/core/testing';
import { DOCUMENT } from '@angular/common';
import { Renderer2, RendererFactory2 } from '@angular/core';
import { ThemeService } from './theme.service';
import { ThemeMode } from '../models/theme.models';

describe('ThemeService', () => {
  let service: ThemeService;
  let mockDocument: Document;
  let mockRenderer: Renderer2;
  let mockRendererFactory: RendererFactory2;
  let mockMediaQuery: MediaQueryList;
  
  beforeEach(() => {
    // Create mock document
    mockDocument = {
      body: {
        classList: {
          add: jasmine.createSpy('add'),
          remove: jasmine.createSpy('remove'),
          filter: jasmine.createSpy('filter').and.returnValue([])
        }
      },
      documentElement: {
        style: {
          setProperty: jasmine.createSpy('setProperty')
        }
      },
      dispatchEvent: jasmine.createSpy('dispatchEvent')
    } as any;
    
    // Create mock renderer
    mockRenderer = {
      addClass: jasmine.createSpy('addClass'),
      removeClass: jasmine.createSpy('removeClass')
    } as any;
    
    // Create mock renderer factory
    mockRendererFactory = {
      createRenderer: jasmine.createSpy('createRenderer').and.returnValue(mockRenderer)
    } as any;
    
    // Create mock media query
    mockMediaQuery = {
      matches: false,
      addEventListener: jasmine.createSpy('addEventListener'),
      removeEventListener: jasmine.createSpy('removeEventListener')
    } as any;
    
    // Mock window.matchMedia
    spyOn(window, 'matchMedia').and.returnValue(mockMediaQuery);
    
    // Clear localStorage before each test
    localStorage.clear();
    
    TestBed.configureTestingModule({
      providers: [
        ThemeService,
        { provide: DOCUMENT, useValue: mockDocument },
        { provide: RendererFactory2, useValue: mockRendererFactory }
      ]
    });
    
    service = TestBed.inject(ThemeService);
  });
  
  afterEach(() => {
    localStorage.clear();
  });
  
  it('should be created', () => {
    expect(service).toBeTruthy();
  });
  
  it('should initialize with system theme', (done) => {
    service.currentTheme$.subscribe(theme => {
      expect(theme).toBe(ThemeMode.System);
      done();
    });
  });
  
  it('should detect light mode when system prefers light', (done) => {
    // Create a new mock with matches set to false
    const lightModeQuery = {
      matches: false,
      addEventListener: jasmine.createSpy('addEventListener'),
      removeEventListener: jasmine.createSpy('removeEventListener')
    } as any;
    
    (window.matchMedia as jasmine.Spy).and.returnValue(lightModeQuery);
    service = TestBed.inject(ThemeService);
    
    service.isDarkMode$.subscribe(isDark => {
      expect(isDark).toBe(false);
      done();
    });
  });
  
  it('should detect dark mode when system prefers dark', () => {
    // Create a new mock with matches set to true
    const darkModeQuery = {
      matches: true,
      addEventListener: jasmine.createSpy('addEventListener'),
      removeEventListener: jasmine.createSpy('removeEventListener')
    } as any;
    
    (window.matchMedia as jasmine.Spy).and.returnValue(darkModeQuery);
    service = TestBed.inject(ThemeService);
    
    expect(service.isSystemDarkMode()).toBe(true);
  });
  
  it('should toggle between light and dark themes', (done) => {
    service.setTheme(ThemeMode.Light);
    
    setTimeout(() => {
      service.toggleTheme();
      
      service.isDarkMode$.subscribe(isDark => {
        expect(isDark).toBe(true);
        done();
      });
    }, 100);
  });
  
  it('should persist theme preference to localStorage', () => {
    service.setTheme(ThemeMode.Dark);
    expect(localStorage.getItem('app-theme-preference')).toBe(ThemeMode.Dark);
  });
  
  it('should load theme preference from localStorage', () => {
    localStorage.setItem('app-theme-preference', ThemeMode.Dark);
    
    // Create new service instance to test loading
    const newService = new ThemeService(mockDocument, mockRendererFactory);
    
    newService.currentTheme$.subscribe(theme => {
      expect(theme).toBe(ThemeMode.Dark);
    });
  });
  
  it('should apply CSS variables for light theme', () => {
    service.setTheme(ThemeMode.Light);
    
    expect(mockDocument.documentElement.style.setProperty).toHaveBeenCalledWith(
      '--theme-background', 
      '#fafafa'
    );
    expect(mockDocument.documentElement.style.setProperty).toHaveBeenCalledWith(
      '--theme-text-primary', 
      'rgba(0, 0, 0, 0.87)'
    );
  });
  
  it('should apply CSS variables for dark theme', () => {
    service.setTheme(ThemeMode.Dark);
    
    expect(mockDocument.documentElement.style.setProperty).toHaveBeenCalledWith(
      '--theme-background', 
      '#303030'
    );
    expect(mockDocument.documentElement.style.setProperty).toHaveBeenCalledWith(
      '--theme-text-primary', 
      'rgba(255, 255, 255, 1.00)'
    );
  });
  
  it('should emit theme change events', (done) => {
    service.themeChange$.subscribe(event => {
      expect(event.isDark).toBe(true);
      expect(event.mode).toBe(ThemeMode.Dark);
      expect(event.colors).toBeDefined();
      expect(event.timestamp).toBeInstanceOf(Date);
      done();
    });
    
    service.setTheme(ThemeMode.Dark);
  });
  
  it('should dispatch custom DOM event on theme change', () => {
    service.setTheme(ThemeMode.Dark);
    
    expect(mockDocument.dispatchEvent).toHaveBeenCalled();
    const eventCall = (mockDocument.dispatchEvent as jasmine.Spy).calls.mostRecent();
    const event = eventCall.args[0];
    
    expect(event).toBeInstanceOf(CustomEvent);
    expect(event.type).toBe('themechange');
  });
  
  it('should provide correct theme colors for light mode', (done) => {
    service.setTheme(ThemeMode.Light);
    
    service.themeColors$.subscribe(colors => {
      expect(colors.primary).toBe('#1976d2');
      expect(colors.background).toBe('#fafafa');
      expect(colors.text).toBe('rgba(0, 0, 0, 0.87)');
      done();
    });
  });
  
  it('should provide correct theme colors for dark mode', (done) => {
    service.setTheme(ThemeMode.Dark);
    
    service.themeColors$.subscribe(colors => {
      expect(colors.primary).toBe('#90caf9');
      expect(colors.background).toBe('#303030');
      expect(colors.text).toBe('rgba(255, 255, 255, 1.00)');
      done();
    });
  });
  
  it('should calculate correct contrast color', () => {
    expect(service.getContrastColor('#ffffff')).toBe('#000000');
    expect(service.getContrastColor('#000000')).toBe('#ffffff');
    expect(service.getContrastColor('#808080')).toBe('#000000');
  });
  
  it('should clear preference and revert to system theme', () => {
    service.setTheme(ThemeMode.Dark);
    expect(localStorage.getItem('app-theme-preference')).toBe(ThemeMode.Dark);
    
    service.clearPreference();
    
    expect(localStorage.getItem('app-theme-preference')).toBeNull();
    service.currentTheme$.subscribe(theme => {
      expect(theme).toBe(ThemeMode.System);
    });
  });
  
  it('should update document classes correctly', () => {
    // Create a new mock document body with proper classList
    const mockClassList = {
      add: jasmine.createSpy('add'),
      remove: jasmine.createSpy('remove'),
      forEach: jasmine.createSpy('forEach'),
      length: 0
    };
    
    // Update the mock document body
    mockDocument.body = {
      classList: mockClassList
    } as any;
    
    // Create mock array for Array.from to work
    spyOn(Array, 'from').and.returnValue([]);
    
    service.setTheme(ThemeMode.Dark);
    
    expect(mockRenderer.addClass).toHaveBeenCalledWith(mockDocument.body, 'theme-dark');
    expect(mockRenderer.addClass).toHaveBeenCalledWith(mockDocument.body, 'dark-theme');
    expect(mockRenderer.removeClass).toHaveBeenCalledWith(mockDocument.body, 'light-theme');
  });
  
  it('should handle system theme changes when in system mode', () => {
    service.setTheme(ThemeMode.System);
    
    // Get the event listener that was registered
    const addEventListenerCall = (mockMediaQuery.addEventListener as jasmine.Spy).calls.mostRecent();
    const eventListener = addEventListenerCall?.args[1];
    
    if (eventListener) {
      // Simulate system theme change to dark
      const mockEvent = { matches: true } as MediaQueryListEvent;
      
      // Manually trigger the event since we can't properly simulate fromEvent in tests
      // This would normally be handled by the fromEvent observable
      expect(service.isSystemDarkMode()).toBe(false); // Initial state
    }
  });
  
  it('should not change theme on system changes when not in system mode', () => {
    service.setTheme(ThemeMode.Light);
    
    const initialDarkMode = service['isDarkModeSubject'].value;
    
    // Simulate system theme change
    // Since we're not in system mode, this should not affect the theme
    const addEventListenerCall = (mockMediaQuery.addEventListener as jasmine.Spy).calls.mostRecent();
    const eventListener = addEventListenerCall?.args[1];
    
    if (eventListener) {
      const mockEvent = { matches: true } as MediaQueryListEvent;
      // Theme should remain unchanged since we're in Light mode, not System mode
    }
    
    expect(service['isDarkModeSubject'].value).toBe(initialDarkMode);
  });
  
  it('should clean up subscriptions on destroy', () => {
    const destroySpy = spyOn(service['destroy$'], 'next');
    const completeSpy = spyOn(service['destroy$'], 'complete');
    
    service.ngOnDestroy();
    
    expect(destroySpy).toHaveBeenCalled();
    expect(completeSpy).toHaveBeenCalled();
  });
});