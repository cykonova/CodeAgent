import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-progress-indicator',
  standalone: true,
  imports: [CommonModule, MatProgressBarModule, MatProgressSpinnerModule, MatIconModule],
  templateUrl: './progress-indicator.component.html',
  styleUrls: ['./progress-indicator.component.scss']
})
export class ProgressIndicatorComponent {
  @Input() type: 'linear' | 'circular' | 'steps' = 'linear';
  @Input() mode: 'determinate' | 'indeterminate' = 'determinate';
  @Input() value = 0;
  @Input() color: 'primary' | 'accent' | 'warn' = 'primary';
  @Input() diameter = 40;
  @Input() showInfo = true;
  @Input() label = 'Progress';
  @Input() steps: string[] = [];
  @Input() currentStep = 0;
}