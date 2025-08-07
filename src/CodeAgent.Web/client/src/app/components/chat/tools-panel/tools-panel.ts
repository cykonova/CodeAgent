import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

interface Tool {
  id: string;
  name: string;
  description: string;
  icon: string;
  category: 'code' | 'file' | 'git' | 'ai' | 'debug';
  enabled: boolean;
  shortcut?: string;
  status?: 'idle' | 'running' | 'error';
  lastUsed?: Date;
}

interface ToolCategory {
  name: string;
  icon: string;
  tools: Tool[];
  expanded: boolean;
}

@Component({
  selector: 'app-tools-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatExpansionModule,
    MatChipsModule,
    MatBadgeModule,
    MatTooltipModule,
    MatDividerModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './tools-panel.html',
  styleUrl: './tools-panel.scss'
})
export class ToolsPanel implements OnInit {
  loading = signal(false);
  activeTools = signal<string[]>([]);
  
  tools = signal<Tool[]>([
    // Code Tools
    {
      id: 'search-code',
      name: 'Search Code',
      description: 'Search across all files in the project',
      icon: 'search',
      category: 'code',
      enabled: true,
      shortcut: 'Ctrl+Shift+F',
      status: 'idle'
    },
    {
      id: 'find-replace',
      name: 'Find & Replace',
      description: 'Find and replace text across multiple files',
      icon: 'find_replace',
      category: 'code',
      enabled: true,
      shortcut: 'Ctrl+H',
      status: 'idle'
    },
    {
      id: 'format-code',
      name: 'Format Code',
      description: 'Auto-format code using project standards',
      icon: 'auto_fix_high',
      category: 'code',
      enabled: true,
      shortcut: 'Shift+Alt+F',
      status: 'idle'
    },
    {
      id: 'analyze-imports',
      name: 'Analyze Imports',
      description: 'Find unused imports and optimize dependencies',
      icon: 'account_tree',
      category: 'code',
      enabled: true,
      status: 'idle'
    },
    {
      id: 'generate-docs',
      name: 'Generate Docs',
      description: 'Auto-generate documentation from code comments',
      icon: 'article',
      category: 'code',
      enabled: true,
      status: 'idle'
    },
    
    // File Tools
    {
      id: 'create-file',
      name: 'Create File',
      description: 'Create new files with templates',
      icon: 'note_add',
      category: 'file',
      enabled: true,
      shortcut: 'Ctrl+N',
      status: 'idle'
    },
    {
      id: 'create-folder',
      name: 'Create Folder',
      description: 'Create new directories in the project',
      icon: 'create_new_folder',
      category: 'file',
      enabled: true,
      status: 'idle'
    },
    {
      id: 'move-rename',
      name: 'Move/Rename',
      description: 'Move or rename files and update references',
      icon: 'drive_file_move',
      category: 'file',
      enabled: true,
      status: 'idle'
    },
    {
      id: 'duplicate-file',
      name: 'Duplicate',
      description: 'Duplicate files or folders',
      icon: 'content_copy',
      category: 'file',
      enabled: true,
      status: 'idle'
    },
    
    // Git Tools
    {
      id: 'git-status',
      name: 'Git Status',
      description: 'View current git status and changes',
      icon: 'info',
      category: 'git',
      enabled: true,
      shortcut: 'Ctrl+Shift+G',
      status: 'idle'
    },
    {
      id: 'git-diff',
      name: 'View Diff',
      description: 'Compare changes between commits',
      icon: 'difference',
      category: 'git',
      enabled: true,
      status: 'idle'
    },
    {
      id: 'git-commit',
      name: 'Commit Changes',
      description: 'Stage and commit changes',
      icon: 'check_circle',
      category: 'git',
      enabled: true,
      shortcut: 'Ctrl+Enter',
      status: 'idle'
    },
    {
      id: 'git-branch',
      name: 'Branch Manager',
      description: 'Create, switch, and manage branches',
      icon: 'call_split',
      category: 'git',
      enabled: true,
      status: 'idle'
    },
    
    // AI Tools
    {
      id: 'explain-code',
      name: 'Explain Code',
      description: 'Get AI explanations of selected code',
      icon: 'psychology',
      category: 'ai',
      enabled: true,
      status: 'idle',
      lastUsed: new Date(Date.now() - 30 * 60 * 1000)
    },
    {
      id: 'suggest-improvements',
      name: 'Code Suggestions',
      description: 'Get AI suggestions for code improvements',
      icon: 'lightbulb',
      category: 'ai',
      enabled: true,
      status: 'idle',
      lastUsed: new Date(Date.now() - 45 * 60 * 1000)
    },
    {
      id: 'generate-tests',
      name: 'Generate Tests',
      description: 'Auto-generate unit tests for functions',
      icon: 'quiz',
      category: 'ai',
      enabled: true,
      status: 'idle'
    },
    {
      id: 'code-review',
      name: 'AI Code Review',
      description: 'Get automated code review feedback',
      icon: 'rate_review',
      category: 'ai',
      enabled: true,
      status: 'idle'
    },
    
    // Debug Tools
    {
      id: 'syntax-check',
      name: 'Syntax Check',
      description: 'Check for syntax errors in code',
      icon: 'bug_report',
      category: 'debug',
      enabled: true,
      status: 'idle'
    },
    {
      id: 'lint-code',
      name: 'Lint Code',
      description: 'Run linting tools on the codebase',
      icon: 'rule',
      category: 'debug',
      enabled: true,
      status: 'idle'
    },
    {
      id: 'run-tests',
      name: 'Run Tests',
      description: 'Execute unit and integration tests',
      icon: 'play_arrow',
      category: 'debug',
      enabled: true,
      shortcut: 'Ctrl+Shift+T',
      status: 'idle'
    }
  ]);
  
  categories = signal<ToolCategory[]>([]);
  
  ngOnInit(): void {
    this.organizeTools();
  }
  
  private organizeTools(): void {
    const toolsByCategory = new Map<string, Tool[]>();
    
    // Group tools by category
    this.tools().forEach(tool => {
      if (!toolsByCategory.has(tool.category)) {
        toolsByCategory.set(tool.category, []);
      }
      toolsByCategory.get(tool.category)!.push(tool);
    });
    
    // Create category objects
    const categories: ToolCategory[] = [
      {
        name: 'Code Tools',
        icon: 'code',
        tools: toolsByCategory.get('code') || [],
        expanded: true
      },
      {
        name: 'File Operations',
        icon: 'folder',
        tools: toolsByCategory.get('file') || [],
        expanded: false
      },
      {
        name: 'Git Integration',
        icon: 'source',
        tools: toolsByCategory.get('git') || [],
        expanded: false
      },
      {
        name: 'AI Assistance',
        icon: 'psychology',
        tools: toolsByCategory.get('ai') || [],
        expanded: false
      },
      {
        name: 'Debug & Test',
        icon: 'bug_report',
        tools: toolsByCategory.get('debug') || [],
        expanded: false
      }
    ];
    
    this.categories.set(categories);
  }
  
  executeTool(tool: Tool): void {
    if (!tool.enabled) return;
    
    // Mark tool as running
    this.updateToolStatus(tool.id, 'running');
    this.activeTools.update(active => [...active, tool.id]);
    
    // Simulate tool execution
    console.log(`Executing tool: ${tool.name}`);
    
    // TODO: Implement actual tool execution
    setTimeout(() => {
      this.updateToolStatus(tool.id, 'idle');
      this.activeTools.update(active => active.filter(id => id !== tool.id));
      
      // Update last used time
      this.tools.update(tools =>
        tools.map(t =>
          t.id === tool.id ? { ...t, lastUsed: new Date() } : t
        )
      );
    }, 2000);
  }
  
  private updateToolStatus(toolId: string, status: 'idle' | 'running' | 'error'): void {
    this.tools.update(tools =>
      tools.map(tool =>
        tool.id === toolId ? { ...tool, status } : tool
      )
    );
    this.organizeTools(); // Refresh categories
  }
  
  toggleTool(toolId: string): void {
    this.tools.update(tools =>
      tools.map(tool =>
        tool.id === toolId ? { ...tool, enabled: !tool.enabled } : tool
      )
    );
    this.organizeTools(); // Refresh categories
  }
  
  getEnabledToolsCount(category: ToolCategory): number {
    return category.tools.filter(tool => tool.enabled).length;
  }
  
  getRunningToolsCount(category: ToolCategory): number {
    return category.tools.filter(tool => tool.status === 'running').length;
  }
  
  formatLastUsed(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays}d ago`;
  }
  
  refreshTools(): void {
    this.loading.set(true);
    // TODO: Refresh tool availability from backend
    setTimeout(() => {
      this.loading.set(false);
    }, 1000);
  }

  getTotalEnabledToolsCount(): number {
    return this.tools().filter(t => t.enabled).length;
  }

  getTotalToolsCount(): number {
    return this.tools().length;
  }

  getActiveToolsCount(): number {
    return this.activeTools().length;
  }

  toggleAllTools(): void {
    const allEnabled = this.tools().every(t => t.enabled);
    this.tools.update(tools => 
      tools.map(t => ({ ...t, enabled: !allEnabled }))
    );
    this.organizeTools();
  }
}