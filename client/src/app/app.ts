import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './services/theme';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet></router-outlet>',
  styles: [':host { display: block; height: 100%; }']
})
export class App implements OnInit {
  private themeService = inject(ThemeService);
  
  ngOnInit(): void {
    // Theme service will automatically apply the saved theme
  }
}