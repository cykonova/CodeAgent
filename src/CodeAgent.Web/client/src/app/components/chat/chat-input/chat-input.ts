import { Component, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-chat-input',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule],
  templateUrl: './chat-input.html',
  styleUrl: './chat-input.scss'
})
export class ChatInputComponent {
  @Input() disabled: boolean = false;
  @Input() placeholder: string = 'Type your message... (Enter to send, Shift+Enter for new line)';
  
  @Output() messageSent = new EventEmitter<string>();
  
  inputMessage: string = '';
  
  handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
  
  sendMessage(): void {
    if (this.inputMessage.trim() && !this.disabled) {
      this.messageSent.emit(this.inputMessage);
      this.inputMessage = '';
    }
  }
}