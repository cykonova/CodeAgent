import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { ModelInfo } from '../components/configuration/provider-config/provider-config';

export interface ModelInstallProgress {
  modelId: string;
  status: string;
  percentComplete: number;
  bytesDownloaded?: number;
  totalBytes?: number;
  currentOperation?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ModelService {
  private apiUrl = `${environment.apiUrl}/api/models`;
  
  constructor(private http: HttpClient) {}
  
  /**
   * List all models for a provider
   */
  listModels(providerId: string): Observable<ModelInfo[]> {
    return this.http.get<ModelInfo[]>(`${this.apiUrl}/${providerId}/list`);
  }
  
  /**
   * Search for models
   */
  searchModels(providerId: string, query: string): Observable<ModelInfo[]> {
    return this.http.get<ModelInfo[]>(`${this.apiUrl}/${providerId}/search`, {
      params: { q: query }
    });
  }
  
  /**
   * Get model details
   */
  getModelInfo(providerId: string, modelId: string): Observable<ModelInfo> {
    return this.http.get<ModelInfo>(`${this.apiUrl}/${providerId}/model/${modelId}`);
  }
  
  /**
   * Install a model with progress tracking
   */
  installModel(
    providerId: string, 
    modelId: string, 
    progressCallback?: (progress: ModelInstallProgress) => void
  ): Observable<any> {
    const subject = new Subject<any>();
    
    // Use EventSource for progress streaming
    const eventSource = new EventSource(
      `${this.apiUrl}/${providerId}/install/${modelId}`
    );
    
    eventSource.onmessage = (event) => {
      try {
        const progress = JSON.parse(event.data) as ModelInstallProgress;
        if (progressCallback) {
          progressCallback(progress);
        }
        
        if (progress.percentComplete >= 100) {
          eventSource.close();
          subject.next({ success: true, modelId });
          subject.complete();
        }
      } catch (error) {
        console.error('Failed to parse progress:', error);
      }
    };
    
    eventSource.onerror = (error) => {
      eventSource.close();
      subject.error(error);
    };
    
    return subject.asObservable();
  }
  
  /**
   * Uninstall a model
   */
  uninstallModel(providerId: string, modelId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${providerId}/uninstall/${modelId}`);
  }
  
  /**
   * Get current/default model for a provider
   */
  getCurrentModel(providerId: string): Observable<string> {
    return this.http.get<string>(`${this.apiUrl}/${providerId}/current`);
  }
  
  /**
   * Set the current/default model for a provider
   */
  setCurrentModel(providerId: string, modelId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${providerId}/current`, { modelId });
  }
  
  /**
   * Check if a model is installed
   */
  isModelInstalled(providerId: string, modelId: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/${providerId}/installed/${modelId}`);
  }
  
  /**
   * Get available Docker LLM models
   */
  getDockerModels(): Observable<ModelInfo[]> {
    return this.http.get<ModelInfo[]>(`${this.apiUrl}/docker/available`);
  }
  
  /**
   * Get Ollama library models
   */
  getOllamaLibrary(): Observable<ModelInfo[]> {
    return this.http.get<ModelInfo[]>(`${this.apiUrl}/ollama/library`);
  }
}