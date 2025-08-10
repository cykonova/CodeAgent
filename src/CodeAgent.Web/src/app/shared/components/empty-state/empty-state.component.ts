import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.scss']
})
export class EmptyStateComponent {
  @Input() icon = 'inbox';
  @Input() title = 'No data available';
  @Input() message = '';
  @Input() actionLabel = '';
  @Input() actionIcon = '';
  @Input() actionColor: 'primary' | 'accent' = 'primary';
  @Output() actionClick = new EventEmitter<void>();
}