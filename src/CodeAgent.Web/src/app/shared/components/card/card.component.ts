import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

export type CardElevation = 'flat' | 'raised' | 'stroked';
export type CardPadding = 'none' | 'small' | 'medium' | 'large';
export type ActionsAlign = 'start' | 'end';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  templateUrl: './card.component.html',
  styleUrl: './card.component.scss'
})
export class CardComponent {
  @Input() title?: string;
  @Input() subtitle?: string;
  @Input() elevation: CardElevation = 'raised';
  @Input() padding: CardPadding = 'medium';
  @Input() showActions = false;
  @Input() actionsAlign: ActionsAlign = 'end';
  @Input() clickable = false;
  @Input() showTools = false;

  get cardClasses(): Record<string, boolean> {
    return {
      [`elevation-${this.elevation}`]: true,
      [`padding-${this.padding}`]: true,
      'clickable': this.clickable
    };
  }
}
