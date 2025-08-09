import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:5000/api'; // Configure based on environment

  constructor(private http: HttpClient) {}

  get<T>(path: string, params?: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}${path}`, { params })
      .pipe(catchError(this.handleError));
  }

  post<T>(path: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${path}`, body)
      .pipe(catchError(this.handleError));
  }

  put<T>(path: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${path}`, body)
      .pipe(catchError(this.handleError));
  }

  delete<T>(path: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${path}`)
      .pipe(catchError(this.handleError));
  }

  patch<T>(path: string, body: any): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}${path}`, body)
      .pipe(catchError(this.handleError));
  }

  private handleError(error: any): Observable<never> {
    console.error('API Error:', error);
    return throwError(() => error.error?.message || 'An error occurred');
  }
}