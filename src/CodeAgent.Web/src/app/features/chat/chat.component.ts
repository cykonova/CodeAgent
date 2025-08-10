import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="chat-container">
      <h1 class="page-title">Agent Chat</h1>
      
      <mat-card>
        <mat-card-content>
          <p>Chat interface will be implemented here.</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .chat-container {
      max-width: var(--container-lg);
      margin: 0 auto;
    }
    
    .page-title {
      font-size: 32px;
      font-weight: var(--font-weight-light);
      margin-bottom: var(--spacing-lg);
      color: var(--mat-on-surface);
    }
  `]
})
export class ChatComponent {}