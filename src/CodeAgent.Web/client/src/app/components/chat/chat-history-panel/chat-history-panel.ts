import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

interface ChatSession {
  id: string;
  title: string;
  preview: string;
  messageCount: number;
  lastActivity: Date;
  tags: string[];
  starred: boolean;
  archived: boolean;
}

@Component({
  selector: 'app-chat-history-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatChipsModule,
    MatDividerModule,
    MatMenuModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './chat-history-panel.html',
  styleUrl: './chat-history-panel.scss'
})
export class ChatHistoryPanel implements OnInit {
  searchQuery = signal('');
  loading = signal(false);
  selectedFilter = signal<'all' | 'starred' | 'recent' | 'archived'>('all');

  chatSessions = signal<ChatSession[]>([]);
  filteredSessions = signal<ChatSession[]>([]);

  ngOnInit(): void {
    this.updateFilteredSessions();
  }

  private updateFilteredSessions(): void {
    let sessions = this.chatSessions();

    // Apply search filter
    const query = this.searchQuery().toLowerCase();
    if (query) {
      sessions = sessions.filter(session =>
        session.title.toLowerCase().includes(query) ||
        session.preview.toLowerCase().includes(query) ||
        session.tags.some(tag => tag.toLowerCase().includes(query))
      );
    }

    // Apply category filter
    switch (this.selectedFilter()) {
      case 'starred':
        sessions = sessions.filter(session => session.starred && !session.archived);
        break;
      case 'recent':
        sessions = sessions.filter(session => {
          const twoDaysAgo = new Date(Date.now() - 2 * 24 * 60 * 60 * 1000);
          return session.lastActivity > twoDaysAgo && !session.archived;
        });
        break;
      case 'archived':
        sessions = sessions.filter(session => session.archived);
        break;
      case 'all':
      default:
        sessions = sessions.filter(session => !session.archived);
        break;
    }

    // Sort by last activity (most recent first)
    sessions.sort((a, b) => b.lastActivity.getTime() - a.lastActivity.getTime());

    this.filteredSessions.set(sessions);
  }

  onSearchChange(): void {
    this.updateFilteredSessions();
  }

  setFilter(filter: 'all' | 'starred' | 'recent' | 'archived'): void {
    this.selectedFilter.set(filter);
    this.updateFilteredSessions();
  }

  toggleStar(sessionId: string, event: Event): void {
    event.stopPropagation();
    this.chatSessions.update(sessions =>
      sessions.map(session =>
        session.id === sessionId
          ? { ...session, starred: !session.starred }
          : session
      )
    );
    this.updateFilteredSessions();
  }

  archiveSession(sessionId: string, event: Event): void {
    event.stopPropagation();
    this.chatSessions.update(sessions =>
      sessions.map(session =>
        session.id === sessionId
          ? { ...session, archived: true }
          : session
      )
    );
    this.updateFilteredSessions();
  }

  restoreSession(sessionId: string, event: Event): void {
    event.stopPropagation();
    this.chatSessions.update(sessions =>
      sessions.map(session =>
        session.id === sessionId
          ? { ...session, archived: false }
          : session
      )
    );
    this.updateFilteredSessions();
  }

  deleteSession(sessionId: string, event: Event): void {
    event.stopPropagation();
    if (confirm('Are you sure you want to delete this chat session? This action cannot be undone.')) {
      this.chatSessions.update(sessions =>
        sessions.filter(session => session.id !== sessionId)
      );
      this.updateFilteredSessions();
    }
  }

  openSession(sessionId: string): void {
    // TODO: Load the selected chat session
    console.log('Opening session:', sessionId);
  }

  formatRelativeTime(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;

    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;

    const diffDays = Math.floor(diffHours / 24);
    if (diffDays < 7) return `${diffDays}d ago`;

    const diffWeeks = Math.floor(diffDays / 7);
    if (diffWeeks < 4) return `${diffWeeks}w ago`;

    const diffMonths = Math.floor(diffDays / 30);
    return `${diffMonths}mo ago`;
  }

  getFilterCount(filter: 'all' | 'starred' | 'recent' | 'archived'): number {
    const sessions = this.chatSessions();
    switch (filter) {
      case 'starred':
        return sessions.filter(s => s.starred && !s.archived).length;
      case 'recent':
        const twoDaysAgo = new Date(Date.now() - 2 * 24 * 60 * 60 * 1000);
        return sessions.filter(s => s.lastActivity > twoDaysAgo && !s.archived).length;
      case 'archived':
        return sessions.filter(s => s.archived).length;
      case 'all':
      default:
        return sessions.filter(s => !s.archived).length;
    }
  }

  refreshHistory(): void {
    this.loading.set(true);
    // TODO: Call API to refresh chat history
    setTimeout(() => {
      this.loading.set(false);
    }, 1000);
  }
}
