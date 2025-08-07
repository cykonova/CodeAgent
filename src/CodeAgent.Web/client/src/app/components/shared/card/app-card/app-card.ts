import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

export interface CardAction {
  icon: string;
  label: string;
  action: string;
  color?: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatButtonModule],
  templateUrl: './app-card.html',
  styleUrl: './app-card.scss'
})
export class AppCardComponent {
  @Input() title: string = '';
  @Input() subtitle?: string;
  @Input() actions: CardAction[] = [];
  @Input() showHeader: boolean = true;
  @Input() showContent: boolean = true;
  @Input() showFooter: boolean = false;
  @Input() loading: boolean = false;
  
  @Output() actionClick = new EventEmitter<string>();
  
  onActionClick(action: string) {
    this.actionClick.emit(action);
  }
}