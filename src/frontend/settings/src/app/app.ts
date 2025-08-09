import { Component } from '@angular/core';
import { SettingsComponent } from './settings.component';

@Component({
  imports: [SettingsComponent],
  selector: 'app-root',
  template: '<app-settings></app-settings>',
  styles: [`
    :host {
      display: block;
      height: 100vh;
    }
  `]
})
export class App {
  title = 'settings';
}
