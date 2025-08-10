# Frontend Reorganization Plan

## Executive Summary

The current frontend structure violates Nx monorepo best practices with duplicate projects, scattered libraries, and inconsistent naming conventions. This document outlines a comprehensive reorganization plan to align with Nx standards and improve maintainability.

## Current State Analysis

### Problems Identified

#### 1. Duplicate Projects
Applications exist in both root level and `apps/` directory:
- `chat/` (root) + `apps/chat/`
- `dashboard/` (root) + `apps/dashboard/`
- `projects/` (root) + `apps/projects/`
- `settings/` (root) + `apps/settings/`

#### 2. Scattered Libraries
Libraries are distributed across multiple locations without clear organization:
- Root level: `auth/`, `ui-components/`, `ui-forms/`, `ui-tables/`, `i18n/`, `websocket/`, `data-access/`
- `libs/` folder: `websocket/`, `core/`, `data-access/`
- Duplicates: `websocket` and `data-access` exist in both locations

#### 3. Inconsistent Path Mappings
TypeScript path aliases lack consistency:
```json
"@code-agent/auth"        // Some use @code-agent
"@src/data-access"        // Others use @src
"@src/ui-components"      // No clear pattern
```

#### 4. Non-Standard Nx Structure
Current structure deviates from Nx best practices:
- Applications should be in `apps/`
- Libraries should be in `libs/`
- No project folders at root level
- Module Federation remotes typically organized under shell or in remotes folder

## Proposed Structure

### Option 1: Standard Nx with Flat Apps (Recommended)

```
src/frontend/
├── apps/
│   ├── shell/              # Host application
│   ├── dashboard/          # Remote: Metrics & monitoring
│   ├── projects/           # Remote: Project management
│   ├── chat/              # Remote: Agent interaction
│   └── settings/          # Remote: Configuration
├── libs/
│   ├── data-access/       # API services & state management
│   │   ├── src/
│   │   │   ├── lib/
│   │   │   │   ├── models/
│   │   │   │   └── services/
│   │   │   └── index.ts
│   │   └── project.json
│   ├── feature/           # Feature-specific libraries
│   │   └── auth/
│   │       ├── src/
│   │       │   ├── lib/
│   │       │   │   ├── components/
│   │       │   │   ├── guards/
│   │       │   │   ├── interceptors/
│   │       │   │   └── services/
│   │       │   └── index.ts
│   │       └── project.json
│   ├── ui/               # UI component libraries
│   │   ├── components/   # General UI components
│   │   │   ├── src/
│   │   │   │   ├── lib/
│   │   │   │   │   ├── card/
│   │   │   │   │   ├── loading-overlay/
│   │   │   │   │   ├── progress-indicator/
│   │   │   │   │   ├── skeleton-loader/
│   │   │   │   │   └── stat-card/
│   │   │   │   └── index.ts
│   │   │   └── project.json
│   │   ├── forms/       # Form components
│   │   │   ├── src/
│   │   │   │   ├── lib/
│   │   │   │   │   └── dynamic-form/
│   │   │   │   └── index.ts
│   │   │   └── project.json
│   │   └── tables/      # Table components
│   │       ├── src/
│   │       │   ├── lib/
│   │       │   │   └── data-table/
│   │       │   └── index.ts
│   │       └── project.json
│   ├── util/            # Utility libraries
│   │   ├── i18n/        # Internationalization
│   │   │   ├── src/
│   │   │   │   ├── lib/
│   │   │   │   │   ├── locales/
│   │   │   │   │   └── styles/
│   │   │   │   └── index.ts
│   │   │   └── project.json
│   │   └── websocket/   # WebSocket service
│   │       ├── src/
│   │       │   ├── lib/
│   │       │   └── index.ts
│   │       └── project.json
│   └── core/           # Core services
│       └── error-handler/
│           ├── src/
│           │   └── lib/
│           └── project.json
├── nx.json
├── tsconfig.base.json
├── package.json
└── jest.preset.js
```

### Option 2: Shell with Remotes Subfolder (Alternative)

```
src/frontend/
├── apps/
│   ├── shell/              # Host application
│   └── remotes/           # All remote applications
│       ├── dashboard/
│       ├── projects/
│       ├── chat/
│       └── settings/
├── libs/                  # (same as Option 1)
```

## Important: Module Federation Preservation

**The proposed changes will NOT break module federation.** The apps will remain federated with the following considerations:

1. **Shell (Host) Configuration**: No changes needed - it references remote URLs, not file paths
2. **Remote Configurations**: Module federation configs need path updates (detailed below)
3. **Webpack Configs**: Already properly configured and will continue to work
4. **Runtime Federation**: Unaffected - uses URLs for remote loading

## Migration Plan

### Phase 1: Preparation
1. **Create backup branch**
   ```bash
   git checkout -b feature/frontend-reorganization
   ```

2. **Document current imports**
   - Scan all TypeScript files for import statements
   - Create mapping of current imports to new imports

### Phase 2: Restructure Directories

#### Step 1: Clean up Apps
```bash
# Remove duplicate app folders at root
rm -rf src/frontend/chat
rm -rf src/frontend/dashboard
rm -rf src/frontend/projects
rm -rf src/frontend/settings

# Keep only apps/ versions
```

#### Step 2: Reorganize Libraries
```bash
# Create new library structure
mkdir -p src/frontend/libs/feature
mkdir -p src/frontend/libs/ui/{components,forms,tables}
mkdir -p src/frontend/libs/util

# Move libraries to new locations
mv src/frontend/auth src/frontend/libs/feature/
mv src/frontend/ui-components/* src/frontend/libs/ui/components/
mv src/frontend/ui-forms/* src/frontend/libs/ui/forms/
mv src/frontend/ui-tables/* src/frontend/libs/ui/tables/
mv src/frontend/i18n src/frontend/libs/util/

# Remove duplicates
rm -rf src/frontend/websocket
rm -rf src/frontend/data-access
```

### Phase 3: Update Configurations

#### CRITICAL: Module Federation Updates
The module federation configs currently reference root-level paths. These MUST be updated:

**Update each remote's module-federation.config.js:**

For `apps/dashboard/module-federation.config.js`:
```javascript
module.exports = {
  name: 'dashboard',
  exposes: {
    './Module': './apps/dashboard/src/remote-entry.ts',  // Updated path
  },
};
```

For `apps/projects/module-federation.config.js`:
```javascript
module.exports = {
  name: 'projects',
  exposes: {
    './Module': './apps/projects/src/remote-entry.ts',  // Updated path
  },
};
```

For `apps/chat/module-federation.config.js`:
```javascript
module.exports = {
  name: 'chat',
  exposes: {
    './Module': './apps/chat/src/remote-entry.ts',  // Updated path
  },
};
```

For `apps/settings/module-federation.config.js`:
```javascript
module.exports = {
  name: 'settings',
  exposes: {
    './Module': './apps/settings/src/remote-entry.ts',  // Updated path
  },
};
```

**The shell's module-federation.config.js remains unchanged** as it references remote URLs, not file paths.

#### Step 1: Update tsconfig.base.json
```json
{
  "compilerOptions": {
    "paths": {
      "@code-agent/data-access": ["libs/data-access/src/index.ts"],
      "@code-agent/feature/auth": ["libs/feature/auth/src/index.ts"],
      "@code-agent/ui/components": ["libs/ui/components/src/index.ts"],
      "@code-agent/ui/forms": ["libs/ui/forms/src/index.ts"],
      "@code-agent/ui/tables": ["libs/ui/tables/src/index.ts"],
      "@code-agent/util/i18n": ["libs/util/i18n/src/index.ts"],
      "@code-agent/util/websocket": ["libs/util/websocket/src/index.ts"],
      "@code-agent/core/error-handler": ["libs/core/error-handler/src/index.ts"]
    }
  }
}
```

#### Step 2: Update project.json files
All project.json files in the apps need updating since they currently use root-level paths:

**Example for `apps/dashboard/project.json`:**
```json
{
  "sourceRoot": "apps/dashboard/src",  // Changed from "dashboard/src"
  "targets": {
    "build": {
      "options": {
        "outputPath": "dist/apps/dashboard",  // Changed from "dist/dashboard"
        "browser": "apps/dashboard/src/main.ts",  // Changed from "dashboard/src/main.ts"
        "tsConfig": "apps/dashboard/tsconfig.app.json",  // Updated path
        "assets": [
          {
            "glob": "**/*",
            "input": "apps/dashboard/public"  // Updated path
          }
        ],
        "styles": ["apps/dashboard/src/styles.scss"]  // Updated path
      }
    }
  }
}
```

Apply similar updates to all remote apps: projects, chat, settings

#### Step 3: Update Module Federation configs
- Update remote paths in shell's module-federation.config.js
- Ensure all remotes point to correct locations

### Phase 4: Update Imports

#### Automated Import Updates
```bash
# Use nx migrate or custom script to update all imports
npx nx migrate --fix-imports

# Or use search/replace patterns:
# Old: import { X } from '@src/ui-components'
# New: import { X } from '@code-agent/ui/components'
```

#### Manual Verification
- Review all TypeScript files for import statements
- Update any missed imports
- Verify all relative imports still work

### Phase 5: Clean Up

#### Remove Unnecessary Files
- All `nx-welcome.ts` files
- Empty test files
- Placeholder components
- Duplicate configuration files

#### Files to Remove:
```
apps/*/src/app/nx-welcome.ts
apps/*/src/app/app.spec.ts (if empty)
libs/*/src/lib/*/*.spec.ts (if empty)
```

### Phase 6: Testing & Validation

#### Build Verification
```bash
# Clean build cache
nx reset

# Build all applications
nx run-many --target=build --all

# Run all tests
nx run-many --target=test --all

# Lint check
nx run-many --target=lint --all

# Serve shell with remotes
nx serve shell --devRemotes=dashboard,projects,chat,settings
```

#### Dependency Graph Check
```bash
# Generate and review dependency graph
nx graph

# Check for circular dependencies
nx lint
```

## Benefits of Reorganization

### 1. **Standards Compliance**
- Aligns with Nx best practices
- Follows Angular/Nx community conventions
- Easier onboarding for new developers

### 2. **Improved Developer Experience**
- Clear separation of concerns
- Predictable file locations
- Consistent import patterns
- Better IDE support and autocomplete

### 3. **Better Scalability**
- Organized library structure supports growth
- Clear boundaries between features
- Easier to add new remotes/features

### 4. **Enhanced Build Performance**
- Optimized module federation setup
- Better caching with proper boundaries
- Faster incremental builds

### 5. **Maintainability**
- Logical grouping of related code
- Easier to update dependencies
- Simplified testing structure

## Risk Mitigation

### Potential Risks
1. **Import Breaking**: Imports may break during migration
   - **Mitigation**: Use automated tools, thorough testing
   
2. **Build Configuration Issues**: Module federation configs may need updates
   - **Mitigation**: Test each remote individually before integration
   
3. **CI/CD Pipeline Breaks**: Build paths may change
   - **Mitigation**: Update CI/CD scripts alongside migration

4. **Developer Disruption**: Active development may be affected
   - **Mitigation**: Perform migration during low-activity period

## Rollback Plan

If issues arise:
1. Keep original branch intact until migration is verified
2. Document all configuration changes
3. Create rollback script to reverse directory changes
4. Test rollback procedure before starting migration

## Timeline Estimate

- **Phase 1 (Preparation)**: 1 hour
- **Phase 2 (Restructure)**: 2-3 hours
- **Phase 3 (Update Configs)**: 2 hours
- **Phase 4 (Update Imports)**: 3-4 hours
- **Phase 5 (Clean Up)**: 1 hour
- **Phase 6 (Testing)**: 2-3 hours

**Total Estimated Time**: 11-16 hours

## Recommendation

**Proceed with Option 1 (Standard Nx with Flat Apps)** as it:
- Follows current Nx best practices
- Provides clearest structure
- Simplifies navigation
- Aligns with Angular ecosystem standards

The flat apps structure is simpler and more commonly used in the Nx community as of 2025, making it easier to find documentation and community support.

## Next Steps

1. Review and approve this plan
2. Schedule migration window
3. Create feature branch
4. Execute migration phases
5. Thorough testing
6. Merge to main branch
7. Update documentation
8. Team notification and training

## Appendix: Command Reference

### Useful Nx Commands
```bash
# Generate new library
nx g @nx/angular:library libs/feature/new-feature

# Move library (with updates)
nx g @nx/angular:move --project=old-name libs/new-path

# Update imports automatically
nx migrate latest

# Check affected projects
nx affected:graph

# Format all files
nx format:write

# Reset cache
nx reset
```

### Git Commands for Migration
```bash
# Create feature branch
git checkout -b feature/frontend-reorganization

# Stage all changes
git add -A

# Commit with detailed message
git commit -m "refactor: reorganize frontend to follow Nx best practices

- Move all apps to apps/ directory
- Consolidate libraries under libs/ with logical grouping
- Standardize import paths with @code-agent prefix
- Remove duplicate projects and unused files
- Update all configurations and imports"

# Push branch
git push origin feature/frontend-reorganization
```