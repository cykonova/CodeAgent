import { Component } from '@angular/core';
import { ChatComponent } from './chat.component';

@Component({
  imports: [ChatComponent],
  selector: 'app-root',
  template: '<app-chat></app-chat>',
  styles: [`
    :host {
      display: block;
      height: 100vh;
    }
  `]
})
export class App {
  title = 'chat';
}
