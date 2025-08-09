import { ErrorHandler, Injectable, inject, NgZone } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private snackBar = inject(MatSnackBar);
  private zone = inject(NgZone);
  
  handleError(error: Error): void {
    // Log error for debugging
    console.error('Global Error Handler:', error);
    
    // Check if it's a chunk load error (common in lazy-loaded apps)
    if (error.message && error.message.includes('ChunkLoadError')) {
      this.showError('Application update available. Please refresh the page.');
      return;
    }
    
    // Check for network errors
    if (error.message && error.message.includes('NetworkError')) {
      this.showError('Network error. Please check your connection.');
      return;
    }
    
    // Handle promise rejection errors
    if (error instanceof PromiseRejectionEvent) {
      console.error('Unhandled Promise Rejection:', error.reason);
      this.showError('An unexpected error occurred. Please try again.');
      return;
    }
    
    // Default error message
    const errorMessage = this.getErrorMessage(error);
    this.showError(errorMessage);
    
    // Report error to monitoring service (if configured)
    this.reportError(error);
  }
  
  private getErrorMessage(error: Error): string {
    if (error.message) {
      // Sanitize error message for user display
      const message = error.message.toLowerCase();
      
      if (message.includes('network')) {
        return 'Network error. Please check your connection.';
      }
      
      if (message.includes('timeout')) {
        return 'Request timed out. Please try again.';
      }
      
      if (message.includes('permission') || message.includes('denied')) {
        return 'Permission denied. Please check your access rights.';
      }
      
      // Don't expose technical details to users
      if (error.stack && process.env['NODE_ENV'] === 'development') {
        return error.message;
      }
    }
    
    return 'An unexpected error occurred. Please try again or contact support.';
  }
  
  private showError(message: string): void {
    // Run inside Angular zone to ensure change detection
    this.zone.run(() => {
      this.snackBar.open(message, 'Dismiss', {
        duration: 7000,
        horizontalPosition: 'end',
        verticalPosition: 'top',
        panelClass: 'error-snackbar'
      });
    });
  }
  
  private reportError(error: Error): void {
    // TODO: Integrate with error monitoring service (Sentry, LogRocket, etc.)
    // This is where you would send error reports to your monitoring service
    
    const errorReport = {
      message: error.message,
      stack: error.stack,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      url: window.location.href
    };
    
    // For now, just log to console in development
    if (process.env['NODE_ENV'] === 'development') {
      console.log('Error Report:', errorReport);
    }
  }
}