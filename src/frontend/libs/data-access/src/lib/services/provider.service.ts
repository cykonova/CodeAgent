import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { 
  Provider, 
  CreateProviderRequest, 
  UpdateProviderRequest,
  ProviderStatus 
} from '../models/provider.model';

@Injectable({
  providedIn: 'root'
})
export class ProviderService {
  private providersSubject = new BehaviorSubject<Provider[]>([]);
  public providers$ = this.providersSubject.asObservable();

  constructor(private api: ApiService) {
    this.loadProviders();
  }

  loadProviders(): void {
    this.api.get<Provider[]>('/providers').subscribe(
      providers => this.providersSubject.next(providers)
    );
  }

  getProviders(): Observable<Provider[]> {
    return this.api.get<Provider[]>('/providers')
      .pipe(tap(providers => this.providersSubject.next(providers)));
  }

  getProvider(id: string): Observable<Provider> {
    return this.api.get<Provider>(`/providers/${id}`);
  }

  createProvider(request: CreateProviderRequest): Observable<Provider> {
    return this.api.post<Provider>('/providers', request)
      .pipe(tap(() => this.loadProviders()));
  }

  updateProvider(id: string, request: UpdateProviderRequest): Observable<Provider> {
    return this.api.put<Provider>(`/providers/${id}`, request)
      .pipe(tap(() => this.loadProviders()));
  }

  deleteProvider(id: string): Observable<void> {
    return this.api.delete<void>(`/providers/${id}`)
      .pipe(tap(() => this.loadProviders()));
  }

  testProvider(id: string): Observable<ProviderStatus> {
    return this.api.post<ProviderStatus>(`/providers/${id}/test`, {});
  }

  getModels(id: string): Observable<any[]> {
    return this.api.get<any[]>(`/providers/${id}/models`);
  }

  enableProvider(id: string): Observable<Provider> {
    return this.updateProvider(id, { enabled: true });
  }

  disableProvider(id: string): Observable<Provider> {
    return this.updateProvider(id, { enabled: false });
  }
}