import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, throwError, timer } from 'rxjs';
import { catchError, retry, mergeMap } from 'rxjs/operators';

export interface RetryConfig {
  maxRetries: number;
  delay: number;
  backoff: number;
  excludedStatusCodes: number[];
}

const DEFAULT_RETRY_CONFIG: RetryConfig = {
  maxRetries: 3,
  delay: 1000,
  backoff: 2,
  excludedStatusCodes: [400, 401, 403, 404, 422]
};

export const errorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<any>,
  next: HttpHandlerFn
): Observable<HttpEvent<any>> => {
  const snackBar = inject(MatSnackBar);
  
  // Skip retry for certain endpoints
  const skipRetryEndpoints = ['/auth/', '/login', '/logout'];
  const shouldSkipRetry = skipRetryEndpoints.some(endpoint => req.url.includes(endpoint));
  
  return next(req).pipe(
    // Retry logic with exponential backoff
    shouldSkipRetry ? catchError(error => handleError(error, snackBar)) :
    retryWithBackoff(DEFAULT_RETRY_CONFIG),
    catchError(error => handleError(error, snackBar))
  );
};

function retryWithBackoff(config: RetryConfig) {
  return (source: Observable<any>) => {
    return source.pipe(
      retry({
        count: config.maxRetries,
        delay: (error, retryCount) => {
          // Don't retry for client errors
          if (error instanceof HttpErrorResponse && 
              config.excludedStatusCodes.includes(error.status)) {
            throw error;
          }
          
          // Calculate delay with exponential backoff
          const delay = config.delay * Math.pow(config.backoff, retryCount - 1);
          console.log(`Retry attempt ${retryCount} after ${delay}ms`);
          
          return timer(delay);
        }
      })
    );
  };
}

function handleError(error: any, snackBar: MatSnackBar): Observable<never> {
  let errorMessage = 'An unexpected error occurred';
  let showSnackbar = true;
  
  if (error instanceof HttpErrorResponse) {
    switch (error.status) {
      case 0:
        errorMessage = 'Unable to connect to server. Please check your internet connection.';
        break;
      case 400:
        errorMessage = error.error?.message || 'Invalid request. Please check your input.';
        break;
      case 401:
        errorMessage = 'Authentication required. Please login.';
        showSnackbar = false; // Auth interceptor will handle redirect
        break;
      case 403:
        errorMessage = 'You do not have permission to perform this action.';
        break;
      case 404:
        errorMessage = 'The requested resource was not found.';
        break;
      case 422:
        errorMessage = error.error?.message || 'Validation error. Please check your input.';
        break;
      case 429:
        errorMessage = 'Too many requests. Please try again later.';
        break;
      case 500:
        errorMessage = 'Internal server error. Please try again later.';
        break;
      case 502:
        errorMessage = 'Bad gateway. The server is temporarily unavailable.';
        break;
      case 503:
        errorMessage = 'Service unavailable. Please try again later.';
        break;
      case 504:
        errorMessage = 'Gateway timeout. The request took too long to process.';
        break;
      default:
        errorMessage = error.error?.message || `Server error: ${error.status}`;
    }
    
    // Log detailed error for debugging
    console.error('HTTP Error:', {
      status: error.status,
      message: error.message,
      url: error.url,
      error: error.error
    });
  } else {
    // Non-HTTP errors
    console.error('Non-HTTP Error:', error);
    
    if (error.message) {
      errorMessage = error.message;
    }
  }
  
  // Show error notification
  if (showSnackbar) {
    snackBar.open(errorMessage, 'Dismiss', {
      duration: 5000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: 'error-snackbar'
    });
  }
  
  return throwError(() => error);
}