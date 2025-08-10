import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './skeleton-loader.component.html',
  styleUrls: ['./skeleton-loader.component.scss']
})
export class SkeletonLoaderComponent {
  @Input() type: 'text' | 'title' | 'avatar' | 'image' | 'card' = 'text';
  @Input() width = '100%';
  @Input() height = 'auto';
  @Input() count = 1;
}