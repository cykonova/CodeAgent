import { Component, Input, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MessageComponent, Message } from '../message/message';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-messages-list',
  standalone: true,
  imports: [CommonModule, MessageComponent, MatProgressSpinnerModule, MatIconModule],
  templateUrl: './messages-list.html',
  styleUrl: './messages-list.scss'
})
export class MessagesListComponent implements AfterViewChecked {
  @Input() messages: Message[] = [];
  @Input() isLoading: boolean = false;
  @Input() showWelcome: boolean = true;
  
  @ViewChild('messagesContainer') private messagesContainer?: ElementRef;
  
  ngAfterViewChecked() {
    this.scrollToBottom();
  }
  
  private scrollToBottom(): void {
    if (this.messagesContainer) {
      try {
        this.messagesContainer.nativeElement.scrollTop = 
          this.messagesContainer.nativeElement.scrollHeight;
      } catch(err) { }
    }
  }
}