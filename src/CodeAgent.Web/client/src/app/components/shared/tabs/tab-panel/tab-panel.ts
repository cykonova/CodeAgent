import { Component, Input, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  selector: 'app-tab-panel',
  standalone: true,
  imports: [CommonModule, MatTabsModule],
  template: `
    <mat-tab [label]="label" [disabled]="disabled">
      <ng-template matTabContent>
        <div class="tab-panel-content">
          <ng-content></ng-content>
        </div>
      </ng-template>
    </mat-tab>
  `,
  styles: [`
    .tab-panel-content {
      padding: 16px;
      height: 100%;
      overflow: auto;
    }
  `]
})
export class TabPanelComponent {
  @Input() label: string = '';
  @Input() disabled: boolean = false;
  @Input() icon?: string;
  
  index: number = 0;
}