import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';

export interface ChipItem {
  id: string;
  label: string;
  icon?: string;
  color?: 'primary' | 'accent' | 'warn';
  removable?: boolean;
  disabled?: boolean;
}

@Component({
  selector: 'app-chip-list',
  standalone: true,
  imports: [CommonModule, MatChipsModule, MatIconModule],
  templateUrl: './chip-list.html',
  styleUrl: './chip-list.scss'
})
export class ChipListComponent {
  @Input() chips: ChipItem[] = [];
  @Input() selectable: boolean = false;
  @Input() multiple: boolean = false;
  @Input() removable: boolean = true;
  @Input() addOnBlur: boolean = true;
  @Input() orientation: 'horizontal' | 'vertical' = 'horizontal';
  
  @Output() chipRemoved = new EventEmitter<ChipItem>();
  @Output() chipSelected = new EventEmitter<ChipItem>();
  @Output() chipAdded = new EventEmitter<string>();
  
  selectedChips: Set<string> = new Set();
  
  onRemove(chip: ChipItem) {
    if (chip.removable !== false) {
      this.chipRemoved.emit(chip);
    }
  }
  
  onSelect(chip: ChipItem) {
    if (this.selectable && !chip.disabled) {
      if (this.multiple) {
        if (this.selectedChips.has(chip.id)) {
          this.selectedChips.delete(chip.id);
        } else {
          this.selectedChips.add(chip.id);
        }
      } else {
        this.selectedChips.clear();
        this.selectedChips.add(chip.id);
      }
      this.chipSelected.emit(chip);
    }
  }
  
  isSelected(chip: ChipItem): boolean {
    return this.selectedChips.has(chip.id);
  }
}