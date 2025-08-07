import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-panel',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatDividerModule],
  templateUrl: './panel.html',
  styleUrl: './panel.scss'
})
export class PanelComponent {
  @Input() title?: string;
  @Input() icon?: string;
  @Input() showHeader: boolean = true;
  @Input() showDivider: boolean = true;
  @Input() collapsible: boolean = false;
  @Input() collapsed: boolean = false;
  @Input() showCloseButton: boolean = false;
  @Input() elevation: number = 0;
  
  @Output() close = new EventEmitter<void>();
  @Output() collapsedChange = new EventEmitter<boolean>();
  
  toggleCollapse() {
    if (this.collapsible) {
      this.collapsed = !this.collapsed;
      this.collapsedChange.emit(this.collapsed);
    }
  }
  
  onClose() {
    this.close.emit();
  }
}