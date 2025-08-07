import { Component, signal, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatTreeModule } from '@angular/material/tree';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { NestedTreeControl } from '@angular/cdk/tree';
import { MatTreeNestedDataSource } from '@angular/material/tree';
import { FileService, FileNode as ApiFileNode, FileContent } from '../../../services/file.service';
import { GitService } from '../../../services/git.service';
import { HttpClient } from '@angular/common/http';

interface FileNode {
  name: string;
  path: string;
  type: 'file' | 'directory';
  size?: number;
  modified?: Date;
  children?: FileNode[];
  expanded?: boolean;
}

interface RecentFile {
  name: string;
  path: string;
  type: string;
  modified: Date;
  size: number;
}

interface ProjectContext {
  projectRoot: string;
  activeFiles: string[];
  pinnedFiles: string[];
  gitBranch: string;
  gitStatus: {
    modified: number;
    staged: number;
    untracked: number;
  };
}

@Component({
  selector: 'app-context-files-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatChipsModule,
    MatDividerModule,
    MatTreeModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatBadgeModule,
    MatSnackBarModule
  ],
  templateUrl: './context-files-panel.html',
  styleUrl: './context-files-panel.scss'
})
export class ContextFilesPanel implements OnInit {
  // Services
  private fileService = inject(FileService);
  private gitService = inject(GitService);
  private snackBar = inject(MatSnackBar);
  private http = inject(HttpClient);

  // Tree control for file browser
  treeControl = new NestedTreeControl<FileNode>(node => node.children);
  dataSource = new MatTreeNestedDataSource<FileNode>();
  
  // State signals
  context = signal<ProjectContext>({
    projectRoot: '',
    activeFiles: [],
    pinnedFiles: this.loadPinnedFiles(),
    gitBranch: 'main',
    gitStatus: {
      modified: 0,
      staged: 0,
      untracked: 0
    }
  });
  
  recentFiles = signal<RecentFile[]>([]);
  fileTree = signal<FileNode[]>([]);
  loading = signal(false);
  
  ngOnInit(): void {
    this.loadProjectContext();
    this.loadFileTree();
    this.loadRecentFiles();
    this.loadGitStatus();
  }
  
  private loadProjectContext(): void {
    // Load project root from configuration or detect from current directory
    this.http.get<any>('http://localhost:5001/api/configuration').subscribe({
      next: (config) => {
        this.context.update(ctx => ({
          ...ctx,
          projectRoot: config.projectDirectory || '.'
        }));
      },
      error: () => {
        // Fallback to current working directory
        this.context.update(ctx => ({
          ...ctx,
          projectRoot: '.'
        }));
      }
    });
  }

  private loadFileTree(): void {
    this.loading.set(true);
    const projectRoot = this.context().projectRoot || '.';
    
    this.fileService.listFiles(projectRoot).subscribe({
      next: (files) => {
        const convertedFiles = this.convertApiNodesToFileNodes(files);
        this.fileTree.set(convertedFiles);
        this.dataSource.data = convertedFiles;
        this.expandDefaultNodes();
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Failed to load file tree:', error);
        this.snackBar.open('Failed to load file tree', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  private loadRecentFiles(): void {
    // Get recent files from localStorage or service
    const stored = localStorage.getItem('codeagent-recent-files');
    if (stored) {
      try {
        const parsed = JSON.parse(stored);
        this.recentFiles.set(parsed.map((f: any) => ({
          ...f,
          modified: new Date(f.modified)
        })));
      } catch (error) {
        console.error('Failed to parse recent files:', error);
      }
    }
  }

  private loadGitStatus(): void {
    this.gitService.getStatus().subscribe({
      next: (status) => {
        this.context.update(ctx => ({
          ...ctx,
          gitBranch: status.branch || 'main',
          gitStatus: {
            modified: status.modified?.length || 0,
            staged: status.staged?.length || 0,
            untracked: status.untracked?.length || 0
          }
        }));
      },
      error: (error) => {
        console.error('Failed to load git status:', error);
      }
    });
  }

  private convertApiNodesToFileNodes(apiNodes: ApiFileNode[]): FileNode[] {
    return apiNodes.map(apiNode => ({
      name: apiNode.name,
      path: apiNode.path,
      type: apiNode.type === 'folder' ? 'directory' : 'file',
      size: apiNode.size,
      modified: apiNode.modified,
      children: apiNode.children ? this.convertApiNodesToFileNodes(apiNode.children) : undefined,
      expanded: false // Will be set based on preferences
    }));
  }

  private expandDefaultNodes(): void {
    // Expand first level directories
    this.fileTree().forEach(node => {
      if (node.type === 'directory' && ['src', 'app', 'components'].includes(node.name)) {
        node.expanded = true;
        this.treeControl.expand(node);
        this.expandChildrenRecursively(node, 2); // Expand 2 levels deep
      }
    });
  }

  private expandChildrenRecursively(node: FileNode, maxDepth: number = 1): void {
    if (maxDepth <= 0 || !node.children) return;
    
    node.children.forEach(child => {
      if (child.type === 'directory' && ['app', 'components', 'services'].includes(child.name)) {
        child.expanded = true;
        this.treeControl.expand(child);
        this.expandChildrenRecursively(child, maxDepth - 1);
      }
    });
  }

  private loadPinnedFiles(): string[] {
    const stored = localStorage.getItem('codeagent-pinned-files');
    if (stored) {
      try {
        return JSON.parse(stored);
      } catch (error) {
        console.error('Failed to parse pinned files:', error);
      }
    }
    return [];
  }

  private savePinnedFiles(): void {
    localStorage.setItem('codeagent-pinned-files', JSON.stringify(this.context().pinnedFiles));
  }

  private addToRecentFiles(filePath: string): void {
    const fileName = filePath.split('/').pop() || 'unknown';
    const extension = fileName.split('.').pop()?.toLowerCase() || '';
    
    const newRecentFile: RecentFile = {
      name: fileName,
      path: filePath,
      type: this.getFileType(extension),
      modified: new Date(),
      size: 0 // Will be updated when file is loaded
    };

    this.recentFiles.update(recent => {
      const filtered = recent.filter(f => f.path !== filePath);
      const updated = [newRecentFile, ...filtered].slice(0, 10); // Keep only 10 recent files
      
      // Save to localStorage
      localStorage.setItem('codeagent-recent-files', JSON.stringify(updated));
      
      return updated;
    });
  }

  private getFileType(extension: string): string {
    switch (extension) {
      case 'ts': return 'typescript';
      case 'js': return 'javascript';
      case 'html': return 'html';
      case 'scss': case 'css': return 'scss';
      case 'json': return 'json';
      case 'md': return 'markdown';
      default: return 'text';
    }
  }

  hasChild = (_: number, node: FileNode) => !!node.children && node.children.length > 0;
  
  getFileIcon(fileName: string, type: 'file' | 'directory'): string {
    if (type === 'directory') {
      return 'folder';
    }
    
    const extension = fileName.split('.').pop()?.toLowerCase();
    switch (extension) {
      case 'ts': return 'code';
      case 'html': return 'web';
      case 'scss': case 'css': return 'palette';
      case 'json': return 'data_object';
      case 'md': return 'article';
      case 'js': return 'javascript';
      default: return 'description';
    }
  }
  
  getFileTypeIcon(type: string): string {
    switch (type) {
      case 'typescript': return 'code';
      case 'scss': return 'palette';
      case 'markdown': return 'article';
      case 'json': return 'data_object';
      default: return 'description';
    }
  }
  
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }
  
  formatRelativeTime(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} min ago`;
    
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays}d ago`;
  }
  
  onFileClick(node: FileNode): void {
    if (node.type === 'file') {
      this.openFile(node.path);
    }
  }
  
  onRecentFileClick(file: RecentFile): void {
    this.openFile(file.path);
  }

  private openFile(filePath: string): void {
    this.loading.set(true);
    
    this.fileService.readFile(filePath).subscribe({
      next: (fileContent) => {
        // Add to recent files
        this.addToRecentFiles(filePath);
        
        // Add to active files if not already there
        this.context.update(ctx => {
          const activeFiles = ctx.activeFiles.includes(filePath) 
            ? ctx.activeFiles 
            : [...ctx.activeFiles, filePath];
          return { ...ctx, activeFiles };
        });

        // TODO: Emit event to parent component to show file content
        console.log('File opened:', filePath, fileContent);
        
        this.snackBar.open(`Opened ${filePath.split('/').pop()}`, 'Close', { duration: 2000 });
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Failed to read file:', error);
        this.snackBar.open('Failed to open file', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }
  
  pinFile(filePath: string): void {
    const pinned = this.context().pinnedFiles;
    if (!pinned.includes(filePath)) {
      this.context.update(ctx => ({
        ...ctx,
        pinnedFiles: [...ctx.pinnedFiles, filePath]
      }));
      this.savePinnedFiles();
      this.snackBar.open('File pinned', 'Close', { duration: 2000 });
    }
  }
  
  unpinFile(filePath: string): void {
    this.context.update(ctx => ({
      ...ctx,
      pinnedFiles: ctx.pinnedFiles.filter(f => f !== filePath)
    }));
    this.savePinnedFiles();
    this.snackBar.open('File unpinned', 'Close', { duration: 2000 });
  }
  
  isFilePinned(filePath: string): boolean {
    return this.context().pinnedFiles.includes(filePath);
  }
  
  refreshFileTree(): void {
    this.loadFileTree();
  }
}