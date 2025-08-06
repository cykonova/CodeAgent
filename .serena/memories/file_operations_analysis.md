# File Operations Analysis

## Current File Operations Implementation

### Domain Layer
**IFileSystemService Interface** (`/src/CodeAgent.Domain/Interfaces/IFileSystemService.cs`):
- Basic CRUD operations: `ReadFileAsync`, `WriteFileAsync`, `DeleteFileAsync`
- File/directory queries: `FileExistsAsync`, `GetFilesAsync`, `GetDirectoriesAsync`
- Project file scanning: `GetProjectFilesAsync` (excludes build directories)
- **Operation Tracking**: `TrackOperationAsync` method exists for change tracking

**FileOperation Model** (`/src/CodeAgent.Domain/Models/FileOperation.cs`):
- Supports various operation types: Read, Write, Create, Delete, Rename, Move
- Tracks original content and new content
- Includes timestamp for operations
- Has proper structure for diff generation

### Infrastructure Layer
**FileSystemService Implementation** (`/src/CodeAgent.Infrastructure/Services/FileSystemService.cs`):
- ✅ **Change Tracking Implemented**: Stores operations in `_operations` list
- ✅ **Original Content Capture**: Captures original content before modifications
- ✅ **Operation History**: `GetOperationHistory()` and `ClearOperationHistory()` methods
- ✅ **Cross-Platform**: Uses proper .NET file APIs

### What's Missing for Phase 2.1 (Diff Preview)

#### 1. Diff Generation Service
- No diff generation functionality exists
- Need service to compare original vs new content
- Should support unified diff format or side-by-side comparison

#### 2. Preview Before Apply Pattern
- Current `WriteFileAsync` immediately writes to disk
- Need "preview mode" where changes are calculated but not applied
- Need user confirmation step before applying changes

#### 3. CLI Integration
- No commands for file modification with preview
- Need command to show diffs before applying
- Need interactive approval flow

#### 4. Enhanced FileOperation Model
- Could benefit from diff content storage
- Line-by-line change tracking
- Better rollback capability

## Recommended Implementation Strategy

1. **Create IDiffService**: Generate unified diffs between content
2. **Enhance IFileSystemService**: Add preview methods that don't immediately write
3. **Add CLI Commands**: File modification commands with diff preview
4. **Update FileOperation**: Store diff information for better tracking
5. **Add Confirmation Flow**: Interactive approval before applying changes