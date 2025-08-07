import { Component, inject, signal, computed, OnInit, OnDestroy, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTreeModule } from '@angular/material/tree';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { NestedTreeControl } from '@angular/cdk/tree';
import { MatTreeNestedDataSource } from '@angular/material/tree';
import { FileService, FileNode } from '../../../services/file.service';
import { Subscription } from 'rxjs';

// Remove duplicate FileNode interface - using the one from FileService

@Component({
  selector: 'app-file-browser',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatTreeModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    MatProgressBarModule,
    MatMenuModule,
    MatInputModule,
    MatFormFieldModule,
    MatChipsModule,
    MatTooltipModule,
    MatDividerModule,
    MatSnackBarModule,
    MatDialogModule
  ],
  templateUrl: './file-browser.html',
  styleUrl: './file-browser.scss'
})
export class FileBrowser implements OnInit, OnDestroy {
  private fileService = inject(FileService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  
  treeControl = new NestedTreeControl<FileNode>(node => node.children);
  dataSource = new MatTreeNestedDataSource<FileNode>();
  
  currentPath = signal('/');
  selectedFile = signal<FileNode | null>(null);
  isLoading = signal(false);
  searchQuery = '';
  files = signal<FileNode[]>([]);
  
  private subscriptions: Subscription[] = [];
  
  ngOnInit(): void {
    // Monitor loading state
    effect(() => {
      const loading = this.fileService.getLoadingState()();
      this.isLoading.set(loading);
    });
    
    // Monitor error state
    effect(() => {
      const error = this.fileService.getErrorState()();
      if (error) {
        this.snackBar.open(error, 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
    
    // Subscribe to file changes (Observable)
    const changesSub = this.fileService.getFileChanges().subscribe(change => {
      if (change) {
        this.refreshFileTree();
      }
    });
    this.subscriptions.push(changesSub);
    
    // Load initial file tree
    this.loadFileTree();
  }
  
  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }
  
  private loadFileTree(): void {
    const currentPath = this.currentPath();
    this.fileService.listFiles(currentPath).subscribe({
      next: (files) => {
        this.files.set(files);
        this.dataSource.data = files;
      },
      error: (error) => {
        console.error('Failed to load file tree:', error);
      }
    });
  }
  
  breadcrumbs = computed(() => {
    const path = this.currentPath();
    if (path === '/') return ['Root'];
    return ['Root', ...path.split('/').filter(p => p)];
  });
  
  constructor() {
    // Constructor is now empty - initialization moved to ngOnInit
  }
  
  hasChild = (_: number, node: FileNode) => node.type === 'folder' && !!node.children && node.children.length > 0;
  
  selectFile(node: FileNode): void {
    if (node.type === 'file') {
      this.selectedFile.set(node);
      this.openFile(node);
    } else {
      this.treeControl.toggle(node);
      // Load children if folder is expanded and children haven't been loaded
      if (this.treeControl.isExpanded(node) && (!node.children || node.children.length === 0)) {
        this.loadFolderContents(node);
      }
    }
  }
  
  private loadFolderContents(node: FileNode): void {
    this.fileService.listFiles(node.path).subscribe({
      next: (children) => {
        node.children = children;
        // Trigger change detection
        this.dataSource.data = [...this.dataSource.data];
      },
      error: (error) => {
        console.error('Failed to load folder contents:', error);
      }
    });
  }
  
  openFile(node: FileNode): void {
    if (node.type === 'file') {
      this.fileService.readFile(node.path).subscribe({
        next: (fileContent) => {
          // TODO: Open file editor with content
          console.log('Opening file:', node.path, fileContent);
          this.snackBar.open(`Opened ${node.name}`, 'Close', { duration: 2000 });
        },
        error: (error) => {
          console.error('Failed to open file:', error);
        }
      });
    }
  }
  
  createNewFile(): void {
    const fileName = prompt('Enter file name:');
    if (fileName) {
      const currentPath = this.currentPath();
      const filePath = currentPath === '/' ? `/${fileName}` : `${currentPath}/${fileName}`;
      
      this.fileService.createFile(filePath, '').subscribe({
        next: () => {
          this.snackBar.open(`File ${fileName} created`, 'Close', { duration: 2000 });
        },
        error: (error) => {
          console.error('Failed to create file:', error);
        }
      });
    }
  }
  
  createNewFolder(): void {
    const folderName = prompt('Enter folder name:');
    if (folderName) {
      // TODO: Implement folder creation API
      console.log('Creating folder:', folderName);
      this.snackBar.open('Folder creation not yet implemented', 'Close', { duration: 2000 });
    }
  }
  
  deleteFile(node: FileNode): void {
    if (confirm(`Are you sure you want to delete ${node.name}?`)) {
      this.fileService.deleteFile(node.path).subscribe({
        next: () => {
          this.snackBar.open(`${node.name} deleted`, 'Close', { duration: 2000 });
        },
        error: (error) => {
          console.error('Failed to delete file:', error);
        }
      });
    }
  }
  
  renameFile(node: FileNode): void {
    const newName = prompt('Enter new name:', node.name);
    if (newName && newName !== node.name) {
      // TODO: Implement file renaming API
      console.log('Renaming:', node.path, 'to:', newName);
      this.snackBar.open('File renaming not yet implemented', 'Close', { duration: 2000 });
    }
  }
  
  refreshFileTree(): void {
    this.loadFileTree();
  }
  
  navigateToBreadcrumb(index: number): void {
    const breadcrumbs = this.breadcrumbs();
    if (index === 0) {
      this.currentPath.set('/');
    } else {
      const path = '/' + breadcrumbs.slice(1, index + 1).join('/');
      this.currentPath.set(path);
    }
    this.loadFileTree();
  }
  
  searchFiles(): void {
    if (!this.searchQuery.trim()) {
      this.loadFileTree();
      return;
    }
    
    this.fileService.searchFiles(this.searchQuery, this.currentPath()).subscribe({
      next: (searchResult) => {
        // Convert search results to FileNode format
        const fileNodes: FileNode[] = searchResult.results.map((result: any) => ({
          name: result.name,
          path: result.path,
          type: 'file' as const,
          extension: this.fileService.getFileExtension(result.name),
          size: 0,
          modified: new Date()
        }));
        
        this.files.set(fileNodes);
        this.dataSource.data = fileNodes;
      },
      error: (error) => {
        console.error('Search failed:', error);
      }
    });
  }
  
  getFileIcon(node: FileNode): string {
    if (node.type === 'folder') {
      return this.treeControl.isExpanded(node) ? 'folder_open' : 'folder';
    }
    
    // Return icon based on file extension
    switch (node.extension) {
      case 'ts':
      case 'js':
        return 'code';
      case 'html':
        return 'html';
      case 'css':
      case 'scss':
        return 'style';
      case 'json':
        return 'data_object';
      case 'md':
        return 'description';
      case 'png':
      case 'jpg':
      case 'gif':
        return 'image';
      default:
        return 'insert_drive_file';
    }
  }
  
  formatFileSize(bytes?: number): string {
    if (!bytes) return '';
    return this.fileService.formatFileSize(bytes);
  }
  
  formatDate(date?: Date): string {
    if (!date) return '';
    return this.fileService.formatDate(date);
  }
  
  isTextFile(node: FileNode): boolean {
    return this.fileService.isTextFile(node.name);
  }
  
  isImageFile(node: FileNode): boolean {
    return this.fileService.isImageFile(node.name);
  }
}