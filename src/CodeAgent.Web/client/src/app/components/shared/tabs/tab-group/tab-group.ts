import { Component, Input, Output, EventEmitter, ContentChildren, QueryList, AfterContentInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { ThemePalette } from '@angular/material/core';
import { TabPanelComponent } from '../tab-panel/tab-panel';

@Component({
  selector: 'app-tab-group',
  standalone: true,
  imports: [CommonModule, MatTabsModule],
  templateUrl: './tab-group.html',
  styleUrl: './tab-group.scss'
})
export class TabGroupComponent implements AfterContentInit {
  @Input() selectedIndex: number = 0;
  @Input() animationDuration: string = '500ms';
  @Input() backgroundColor?: ThemePalette;
  @Input() color?: 'primary' | 'accent' | 'warn';
  
  @Output() selectedIndexChange = new EventEmitter<number>();
  @Output() selectedTabChange = new EventEmitter<any>();
  
  @ContentChildren(TabPanelComponent) tabs!: QueryList<TabPanelComponent>;
  
  ngAfterContentInit() {
    this.tabs.forEach((tab, index) => {
      tab.index = index;
    });
  }
  
  onTabChange(index: number) {
    this.selectedIndex = index;
    this.selectedIndexChange.emit(index);
    const selectedTab = this.tabs.toArray()[index];
    if (selectedTab) {
      this.selectedTabChange.emit(selectedTab);
    }
  }
}