import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';

export interface ListItemAction {
  id: string;
  label: string;
  icon?: string;
  color?: string;
}

@Component({
  selector: 'app-list-item',
  standalone: true,
  imports: [CommonModule, MatListModule, MatIconModule, MatButtonModule, MatMenuModule],
  templateUrl: './list-item.html',
  styleUrl: './list-item.scss'
})
export class ListItemComponent {
  @Input() title: string = '';
  @Input() subtitle?: string;
  @Input() icon?: string;
  @Input() avatar?: string;
  @Input() selected: boolean = false;
  @Input() disabled: boolean = false;
  @Input() actions: ListItemAction[] = [];
  @Input() showActions: boolean = false;
  
  @Output() click = new EventEmitter<void>();
  @Output() actionClick = new EventEmitter<string>();
  
  onClick(event: Event) {
    if (!this.disabled) {
      event.stopPropagation();
      this.click.emit();
    }
  }
  
  onActionClick(actionId: string, event: Event) {
    event.stopPropagation();
    this.actionClick.emit(actionId);
  }
}