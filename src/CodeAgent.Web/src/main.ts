import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

bootstrapApplication(AppComponent, appConfig)
  .then(() => {
    console.log('Code Agent application initialized');
    
    // Remove loading spinner with fade animation
    const loadingElement = document.querySelector('.loading-spinner-container');
    if (loadingElement) {
      loadingElement.classList.add('fade-out');
      
      // Remove from DOM after animation completes
      setTimeout(() => {
        loadingElement.remove();
      }, 300);
    }
  })
  .catch((err) => {
    console.error('Failed to bootstrap application:', err);
    
    // Show error message to user
    const loadingElement = document.querySelector('.loading-spinner-container');
    if (loadingElement) {
      loadingElement.innerHTML = '<div class="error-message">Failed to load application. Please refresh the page.</div>';
    }
  });
