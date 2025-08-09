# Phase 6b: Core Libraries & Services

## Overview
Create the shared libraries that will be used across all remote applications, including UI components, data access services, WebSocket communication, and utility functions.

## Visual Reference
The components and services defined in this phase are used throughout all mockups in [`docs/mockups/`](../mockups/). Key library usage examples:
- **UI Components**: Tables, forms, cards used in dashboard and settings mockups
- **Data Services**: API calls shown in project and chat interfaces
- **WebSocket**: Real-time updates in dashboard and chat mockups
- **State Management**: Cross-application state in all remote modules

## Objectives
- Create reusable UI component libraries using Angular Material
- Implement data access layer with API services
- Setup WebSocket service for real-time communication
- Build utility libraries for common functions
- Establish state management patterns

## Library Structure

### 1. UI Component Libraries

#### Generate UI Libraries
```bash
# Forms library
nx g @nx/angular:library ui-forms --directory=libs/ui/forms \
  --standalone --changeDetection=OnPush

# Tables library  
nx g @nx/angular:library ui-tables --directory=libs/ui/tables \
  --standalone --changeDetection=OnPush

# Cards library
nx g @nx/angular:library ui-cards --directory=libs/ui/cards \
  --standalone --changeDetection=OnPush

# Navigation library
nx g @nx/angular:library ui-navigation --directory=libs/ui/navigation \
  --standalone --changeDetection=OnPush

# Dialogs library
nx g @nx/angular:library ui-dialogs --directory=libs/ui/dialogs \
  --standalone --changeDetection=OnPush
```

#### Form Components Library
```typescript
// libs/ui/forms/src/lib/text-input/text-input.component.ts
@Component({
  selector: 'ui-text-input',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    CommonModule
  ],
  template: `
    <mat-form-field [appearance]="appearance" [class.full-width]="fullWidth">
      <mat-label>{{ label }}</mat-label>
      <input matInput 
             [type]="type"
             [formControl]="control"
             [placeholder]="placeholder"
             [required]="required">
      <mat-icon matPrefix *ngIf="prefixIcon">{{ prefixIcon }}</mat-icon>
      <mat-icon matSuffix *ngIf="suffixIcon">{{ suffixIcon }}</mat-icon>
      <mat-error *ngIf="control.hasError('required')">
        {{ label }} is required
      </mat-error>
      <mat-error *ngIf="control.hasError('email')">
        Invalid email format
      </mat-error>
      <mat-hint *ngIf="hint">{{ hint }}</mat-hint>
    </mat-form-field>
  `,
  styles: [`
    .full-width {
      width: 100%;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TextInputComponent {
  @Input() control!: FormControl;
  @Input() label = '';
  @Input() placeholder = '';
  @Input() type = 'text';
  @Input() hint = '';
  @Input() required = false;
  @Input() prefixIcon?: string;
  @Input() suffixIcon?: string;
  @Input() appearance: MatFormFieldAppearance = 'outline';
  @Input() fullWidth = true;
}
```

#### Table Components Library
```typescript
// libs/ui/tables/src/lib/data-table/data-table.component.ts
@Component({
  selector: 'ui-data-table',
  standalone: true,
  imports: [
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatProgressSpinnerModule,
    CommonModule
  ],
  template: `
    <div class="table-container">
      <mat-table [dataSource]="dataSource" matSort>
        <ng-content></ng-content>
        
        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"
            (click)="rowClick.emit(row)"
            [class.clickable]="clickable">
        </tr>
      </mat-table>
      
      <mat-paginator [pageSizeOptions]="pageSizeOptions"
                     [pageSize]="pageSize"
                     showFirstLastButtons>
      </mat-paginator>
      
      <div *ngIf="loading" class="loading-shade">
        <mat-spinner></mat-spinner>
      </div>
    </div>
  `,
  styles: [`
    .table-container {
      position: relative;
      min-height: 200px;
    }
    .clickable {
      cursor: pointer;
    }
    .clickable:hover {
      background-color: rgba(0, 0, 0, 0.04);
    }
    .loading-shade {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255, 255, 255, 0.9);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DataTableComponent {
  @Input() dataSource!: MatTableDataSource<any>;
  @Input() displayedColumns: string[] = [];
  @Input() pageSizeOptions = [10, 25, 50, 100];
  @Input() pageSize = 25;
  @Input() loading = false;
  @Input() clickable = false;
  @Output() rowClick = new EventEmitter<any>();
  
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  
  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }
}
```

### 2. Data Access Library

#### Generate Data Access Library
```bash
nx g @nx/angular:library data-access --directory=libs/data-access \
  --standalone
```

#### API Service
```typescript
// libs/data-access/src/lib/services/api.service.ts
@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = environment.apiUrl;
  
  constructor(private http: HttpClient) {}
  
  get<T>(endpoint: string, params?: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${endpoint}`, { params })
      .pipe(
        retry(2),
        catchError(this.handleError)
      );
  }
  
  post<T>(endpoint: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body)
      .pipe(
        retry(1),
        catchError(this.handleError)
      );
  }
  
  put<T>(endpoint: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body)
      .pipe(
        retry(1),
        catchError(this.handleError)
      );
  }
  
  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}/${endpoint}`)
      .pipe(
        retry(1),
        catchError(this.handleError)
      );
  }
  
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred';
    
    if (error.error instanceof ErrorEvent) {
      errorMessage = error.error.message;
    } else {
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }
    
    return throwError(() => new Error(errorMessage));
  }
}
```

#### Provider Service
```typescript
// libs/data-access/src/lib/services/provider.service.ts
@Injectable({
  providedIn: 'root'
})
export class ProviderService {
  private providersSubject = new BehaviorSubject<Provider[]>([]);
  providers$ = this.providersSubject.asObservable();
  
  constructor(private api: ApiService) {
    this.loadProviders();
  }
  
  loadProviders(): void {
    this.api.get<Provider[]>('providers').subscribe({
      next: (providers) => this.providersSubject.next(providers),
      error: (error) => console.error('Failed to load providers:', error)
    });
  }
  
  getProvider(id: string): Observable<Provider> {
    return this.api.get<Provider>(`providers/${id}`);
  }
  
  createProvider(provider: CreateProviderDto): Observable<Provider> {
    return this.api.post<Provider>('providers', provider).pipe(
      tap(() => this.loadProviders())
    );
  }
  
  updateProvider(id: string, provider: UpdateProviderDto): Observable<Provider> {
    return this.api.put<Provider>(`providers/${id}`, provider).pipe(
      tap(() => this.loadProviders())
    );
  }
  
  deleteProvider(id: string): Observable<void> {
    return this.api.delete<void>(`providers/${id}`).pipe(
      tap(() => this.loadProviders())
    );
  }
}
```

### 3. WebSocket Library

#### Generate WebSocket Library
```bash
nx g @nx/angular:library websocket --directory=libs/websocket \
  --standalone
```

#### WebSocket Service
```typescript
// libs/websocket/src/lib/websocket.service.ts
@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private socket$: WebSocketSubject<any> | null = null;
  private messagesSubject$ = new Subject<WebSocketMessage>();
  private connectionStatus$ = new BehaviorSubject<ConnectionStatus>('disconnected');
  
  messages$ = this.messagesSubject$.asObservable();
  status$ = this.connectionStatus$.asObservable();
  
  connect(url: string): void {
    if (this.socket$) {
      this.socket$.complete();
    }
    
    this.socket$ = new WebSocketSubject({
      url,
      openObserver: {
        next: () => {
          console.log('WebSocket connected');
          this.connectionStatus$.next('connected');
        }
      },
      closeObserver: {
        next: () => {
          console.log('WebSocket disconnected');
          this.connectionStatus$.next('disconnected');
          this.reconnect(url);
        }
      }
    });
    
    this.socket$.pipe(
      retry({ delay: 5000 }),
      catchError(error => {
        console.error('WebSocket error:', error);
        this.connectionStatus$.next('error');
        return EMPTY;
      })
    ).subscribe(message => {
      this.messagesSubject$.next(message);
    });
  }
  
  send(message: WebSocketMessage): void {
    if (this.socket$ && this.connectionStatus$.value === 'connected') {
      this.socket$.next(message);
    } else {
      console.warn('WebSocket not connected');
    }
  }
  
  disconnect(): void {
    if (this.socket$) {
      this.socket$.complete();
      this.socket$ = null;
      this.connectionStatus$.next('disconnected');
    }
  }
  
  private reconnect(url: string): void {
    setTimeout(() => {
      console.log('Attempting to reconnect...');
      this.connectionStatus$.next('reconnecting');
      this.connect(url);
    }, 5000);
  }
}

// Types
export type ConnectionStatus = 'connected' | 'disconnected' | 'reconnecting' | 'error';

export interface WebSocketMessage {
  type: string;
  payload: any;
  timestamp?: Date;
}
```

### 4. State Management Library

#### Generate State Library
```bash
nx g @nx/angular:library state --directory=libs/state \
  --standalone
```

#### Application State Service
```typescript
// libs/state/src/lib/app-state.service.ts
@Injectable({
  providedIn: 'root'
})
export class AppStateService {
  // User state
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();
  
  // Project state
  private selectedProjectSubject = new BehaviorSubject<Project | null>(null);
  selectedProject$ = this.selectedProjectSubject.asObservable();
  
  // Agent state
  private activeAgentsSubject = new BehaviorSubject<Agent[]>([]);
  activeAgents$ = this.activeAgentsSubject.asObservable();
  
  // UI state
  private loadingSubject = new BehaviorSubject<boolean>(false);
  loading$ = this.loadingSubject.asObservable();
  
  private themeSubject = new BehaviorSubject<'light' | 'dark'>('light');
  theme$ = this.themeSubject.asObservable();
  
  setCurrentUser(user: User | null): void {
    this.currentUserSubject.next(user);
  }
  
  setSelectedProject(project: Project | null): void {
    this.selectedProjectSubject.next(project);
  }
  
  addActiveAgent(agent: Agent): void {
    const agents = [...this.activeAgentsSubject.value, agent];
    this.activeAgentsSubject.next(agents);
  }
  
  removeActiveAgent(agentId: string): void {
    const agents = this.activeAgentsSubject.value.filter(a => a.id !== agentId);
    this.activeAgentsSubject.next(agents);
  }
  
  setLoading(loading: boolean): void {
    this.loadingSubject.next(loading);
  }
  
  toggleTheme(): void {
    const current = this.themeSubject.value;
    this.themeSubject.next(current === 'light' ? 'dark' : 'light');
  }
}
```

### 5. Utility Library

#### Generate Utility Library
```bash
nx g @nx/angular:library utils --directory=libs/utils \
  --standalone
```

#### Common Utilities
```typescript
// libs/utils/src/lib/validators.ts
export class CustomValidators {
  static url(control: AbstractControl): ValidationErrors | null {
    const urlPattern = /^(https?|ftp):\/\/[^\s/$.?#].[^\s]*$/i;
    if (control.value && !urlPattern.test(control.value)) {
      return { url: true };
    }
    return null;
  }
  
  static json(control: AbstractControl): ValidationErrors | null {
    try {
      if (control.value) {
        JSON.parse(control.value);
      }
      return null;
    } catch {
      return { json: true };
    }
  }
}

// libs/utils/src/lib/formatters.ts
export class Formatters {
  static fileSize(bytes: number): string {
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    if (bytes === 0) return '0 Bytes';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  }
  
  static duration(ms: number): string {
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    
    if (hours > 0) {
      return `${hours}h ${minutes % 60}m`;
    } else if (minutes > 0) {
      return `${minutes}m ${seconds % 60}s`;
    } else {
      return `${seconds}s`;
    }
  }
}

// libs/utils/src/lib/debounce.ts
export function debounce(delay: number = 300) {
  return function (target: any, propertyKey: string, descriptor: PropertyDescriptor) {
    let timeout: any;
    const original = descriptor.value;
    
    descriptor.value = function (...args: any[]) {
      clearTimeout(timeout);
      timeout = setTimeout(() => original.apply(this, args), delay);
    };
    
    return descriptor;
  };
}
```

## Library Exports Configuration

```typescript
// libs/ui/forms/src/index.ts
export * from './lib/text-input/text-input.component';
export * from './lib/select-input/select-input.component';
export * from './lib/checkbox-input/checkbox-input.component';
export * from './lib/form-validators';

// libs/data-access/src/index.ts
export * from './lib/services/api.service';
export * from './lib/services/provider.service';
export * from './lib/services/project.service';
export * from './lib/services/agent.service';
export * from './lib/models';

// libs/websocket/src/index.ts
export * from './lib/websocket.service';
export * from './lib/websocket.types';

// libs/state/src/index.ts
export * from './lib/app-state.service';
export * from './lib/state.types';

// libs/utils/src/index.ts
export * from './lib/validators';
export * from './lib/formatters';
export * from './lib/debounce';
```

## Success Criteria
- [ ] UI component libraries created with Angular Material
- [ ] All components are standalone and reusable
- [ ] Data access service with HTTP operations
- [ ] WebSocket service with reconnection logic
- [ ] State management service implemented
- [ ] Utility functions and validators created
- [ ] All libraries properly exported
- [ ] Components follow 100-line limit
- [ ] Change detection set to OnPush

## Next Steps
After completing this phase:
1. Proceed to Phase 6c for remote applications
2. Test library imports in shell application
3. Verify WebSocket connection handling
4. Ensure all Material components styled correctly