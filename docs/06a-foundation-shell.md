# Phase 6a: Foundation & Shell Setup

## Overview
Establish the Nx monorepo foundation, configure Module Federation, and create the shell application that will host all remote modules.

## Visual Reference
See the MockML mockup for the shell application layout: [`docs/mockups/shell/main-layout.mml`](../mockups/shell/main-layout.mml)

## Objectives
- Initialize Nx workspace with Angular
- Configure Module Federation for micro-frontend architecture
- Create the shell application with routing
- Setup Angular Material and theming system
- Establish development tooling and standards

## Implementation Steps

### 1. Nx Workspace Initialization
```bash
# Create new Nx workspace
npx create-nx-workspace@latest code-agent-portal \
  --preset=angular \
  --appName=shell \
  --style=scss \
  --nxCloud=false \
  --packageManager=npm

# Add Module Federation plugin
npm install @nx/angular@latest
nx g @nx/angular:setup-mf shell --mfType=host --routing=true
```

### 2. Shell Application Structure
```
apps/shell/
├── src/
│   ├── app/
│   │   ├── app.component.ts      # Main container
│   │   ├── app.routes.ts         # Route configuration
│   │   ├── app.config.ts         # Application config
│   │   └── remote-entry/
│   │       └── entry.module.ts   # Remote module loading
│   ├── assets/
│   │   └── module-federation.manifest.json
│   ├── environments/
│   ├── styles.scss               # Global styles & theme
│   └── main.ts
├── module-federation.config.ts
└── webpack.config.ts
```

### 3. Angular Material Integration
```typescript
// Install Angular Material
npm install @angular/material @angular/cdk @angular/animations

// Material configuration in app.config.ts
import { provideAnimations } from '@angular/platform-browser/animations';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAnimations(),
    {
      provide: MAT_FORM_FIELD_DEFAULT_OPTIONS,
      useValue: { appearance: 'outline' }
    }
  ]
};
```

### 4. Theme System Setup
```scss
// styles/themes/_light-theme.scss
@use '@angular/material' as mat;

$light-primary: mat.define-palette(mat.$indigo-palette);
$light-accent: mat.define-palette(mat.$pink-palette, A200, A100, A400);
$light-warn: mat.define-palette(mat.$red-palette);

$light-theme: mat.define-light-theme((
  color: (
    primary: $light-primary,
    accent: $light-accent,
    warn: $light-warn,
  ),
  typography: mat.define-typography-config(),
  density: 0,
));

// styles/themes/_dark-theme.scss
$dark-theme: mat.define-dark-theme((
  color: (
    primary: $light-primary,
    accent: $light-accent,
    warn: $light-warn,
  ),
  typography: mat.define-typography-config(),
  density: 0,
));
```

### 5. Module Federation Configuration
```typescript
// module-federation.config.ts
import { ModuleFederationConfig } from '@nx/webpack';

const config: ModuleFederationConfig = {
  name: 'shell',
  remotes: [
    'dashboard',
    'projects', 
    'chat',
    'settings'
  ],
  shared: (libraryName, defaultConfig) => {
    if (libraryName === '@angular/core' || 
        libraryName === '@angular/common' ||
        libraryName === '@angular/material') {
      return {
        ...defaultConfig,
        singleton: true,
        strictVersion: true,
        requiredVersion: 'auto'
      };
    }
    return defaultConfig;
  }
};

export default config;
```

### 6. Routing Configuration
```typescript
// app.routes.ts
import { Route } from '@angular/router';
import { loadRemoteModule } from '@nx/angular/mf';

export const appRoutes: Route[] = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadChildren: () =>
      loadRemoteModule('dashboard', './Routes').then((m) => m.remoteRoutes)
  },
  {
    path: 'projects',
    loadChildren: () =>
      loadRemoteModule('projects', './Routes').then((m) => m.remoteRoutes)
  },
  {
    path: 'chat',
    loadChildren: () =>
      loadRemoteModule('chat', './Routes').then((m) => m.remoteRoutes)
  },
  {
    path: 'settings',
    loadChildren: () =>
      loadRemoteModule('settings', './Routes').then((m) => m.remoteRoutes)
  }
];
```

### 7. Main Layout Component
```typescript
// app.component.ts
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    MatSidenavModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatListModule
  ],
  template: `
    <mat-sidenav-container class="app-container">
      <mat-sidenav #drawer mode="side" opened>
        <mat-nav-list>
          <a mat-list-item routerLink="/dashboard" routerLinkActive="active">
            <mat-icon matListItemIcon>dashboard</mat-icon>
            <span matListItemTitle>Dashboard</span>
          </a>
          <a mat-list-item routerLink="/projects" routerLinkActive="active">
            <mat-icon matListItemIcon>folder</mat-icon>
            <span matListItemTitle>Projects</span>
          </a>
          <a mat-list-item routerLink="/chat" routerLinkActive="active">
            <mat-icon matListItemIcon>chat</mat-icon>
            <span matListItemTitle>Chat</span>
          </a>
          <a mat-list-item routerLink="/settings" routerLinkActive="active">
            <mat-icon matListItemIcon>settings</mat-icon>
            <span matListItemTitle>Settings</span>
          </a>
        </mat-nav-list>
      </mat-sidenav>
      
      <mat-sidenav-content>
        <mat-toolbar color="primary">
          <button mat-icon-button (click)="drawer.toggle()">
            <mat-icon>menu</mat-icon>
          </button>
          <span>Code Agent Portal</span>
          <span class="spacer"></span>
          <button mat-icon-button (click)="toggleTheme()">
            <mat-icon>{{ isDarkMode ? 'light_mode' : 'dark_mode' }}</mat-icon>
          </button>
        </mat-toolbar>
        
        <main class="content">
          <router-outlet></router-outlet>
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .app-container {
      height: 100vh;
    }
    .spacer {
      flex: 1 1 auto;
    }
    .content {
      padding: 20px;
    }
  `]
})
export class AppComponent {
  isDarkMode = false;
  
  toggleTheme() {
    this.isDarkMode = !this.isDarkMode;
    // Theme switching logic
  }
}
```

## Development Tools Setup

### ESLint Configuration
```json
{
  "extends": ["plugin:@nx/angular", "plugin:@angular-eslint/template/process-inline-templates"],
  "rules": {
    "max-lines": ["error", 100],
    "@angular-eslint/component-max-inline-declarations": ["error", { "template": 50 }]
  }
}
```

### Prettier Configuration
```json
{
  "singleQuote": true,
  "printWidth": 100,
  "tabWidth": 2,
  "useTabs": false,
  "semi": true,
  "bracketSpacing": true
}
```

## Success Criteria
- [ ] Nx workspace created with Angular preset
- [ ] Module Federation configured for shell and remotes
- [ ] Angular Material installed and configured
- [ ] Theme system with light/dark mode switching
- [ ] Shell application with navigation sidebar
- [ ] Routing configured for all remote modules
- [ ] Development tools (ESLint, Prettier) configured
- [ ] File size limits enforced (100 lines max)

## Next Steps
After completing this phase:
1. Proceed to Phase 6b for core libraries setup
2. Test Module Federation remote loading
3. Verify theme switching functionality
4. Ensure all Material components render correctly