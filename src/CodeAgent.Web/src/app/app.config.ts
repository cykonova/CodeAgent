import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';

import { routes } from './app.routes';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { WebSocketAuthInterceptor } from './core/interceptors/websocket-auth.interceptor';

// Factory function to initialize WebSocketAuthInterceptor
export function initializeWebSocketAuth(interceptor: WebSocketAuthInterceptor) {
  return () => Promise.resolve();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([])),
    // Set outline as default appearance for all Material form fields
    {
      provide: MAT_FORM_FIELD_DEFAULT_OPTIONS,
      useValue: { appearance: 'outline' }
    },
    // WebSocket Auth Interceptor
    WebSocketAuthInterceptor,
    {
      provide: APP_INITIALIZER,
      useFactory: initializeWebSocketAuth,
      deps: [WebSocketAuthInterceptor],
      multi: true
    }
  ]
};
