# Frontend Organization Analysis

## Current Issues Identified

### 1. Duplicated Projects
- **apps/** and root level duplicates:
  - `chat/` exists at both root and `apps/chat/`
  - `dashboard/` exists at both root and `apps/dashboard/`
  - `projects/` exists at both root and `apps/projects/`
  - `settings/` exists at both root and `apps/settings/`

### 2. Inconsistent Library Organization
- Libraries scattered across multiple locations:
  - `libs/` folder contains some libraries (websocket, core, data-access)
  - Root level contains other libraries (auth, ui-components, ui-forms, ui-tables, i18n, websocket, data-access)
  - Duplicate websocket and data-access in both locations

### 3. Non-Standard Nx Structure
- Standard Nx monorepo should have:
  - All applications in `apps/`
  - All libraries in `libs/`
  - No project folders at root level

### 4. Path Mapping Inconsistencies
- Mixed naming conventions in tsconfig paths:
  - `@code-agent/` prefix for some libs
  - `@src/` prefix for others
  - No clear pattern or consistency

## Recommended Actions

### Relocate
1. Move root-level apps to apps/ folder (remove duplicates)
2. Move all libraries to libs/ folder with consistent structure
3. Consolidate duplicate libraries

### Refactor
1. Standardize path mappings with consistent prefix
2. Update all imports across the codebase
3. Ensure module federation configs are updated

### Remove
1. Delete duplicate folders after consolidation
2. Remove nx-welcome components
3. Clean up unused test files and placeholder components