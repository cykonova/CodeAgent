import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Message } from '../message/message';
import { MessagesListComponent } from '../messages-list/messages-list';

@Component({
  selector: 'app-tool-call-demo',
  standalone: true,
  imports: [CommonModule, MessagesListComponent],
  template: `
    <div class="demo-container">
      <h2>Tool Call Display Demo</h2>
      <app-messages-list 
        [messages]="demoMessages"
        [isLoading]="false"
        [showWelcome]="false">
      </app-messages-list>
    </div>
  `,
  styles: [`
    .demo-container {
      padding: 20px;
      max-width: 800px;
      margin: 0 auto;
      
      h2 {
        margin-bottom: 20px;
      }
    }
  `]
})
export class ToolCallDemoComponent {
  demoMessages: Message[] = [
    {
      id: '1',
      role: 'user',
      content: 'Can you help me analyze this code?',
      timestamp: new Date()
    },
    {
      id: '2',
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      toolCalls: [
        {
          name: 'read_file',
          arguments: {
            path: '/src/main.ts',
            encoding: 'utf-8'
          }
        },
        {
          name: 'analyze_code',
          arguments: {
            language: 'typescript',
            metrics: ['complexity', 'maintainability']
          }
        },
        {
          name: 'respond_to_user',
          arguments: {
            message: 'I\'ve analyzed your code. Here are my findings:\n\n1. The code is well-structured with good separation of concerns\n2. Complexity score: 7/10 (moderate)\n3. Maintainability score: 8/10 (good)\n\nI noticed a few areas for improvement:\n- Consider extracting complex logic into separate functions\n- Add more comprehensive error handling\n- Some functions could benefit from better type annotations'
          }
        }
      ]
    },
    {
      id: '3',
      role: 'user',
      content: 'Can you search for all TypeScript files?',
      timestamp: new Date()
    },
    {
      id: '4',
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      toolCalls: [
        {
          name: 'search',
          arguments: {
            pattern: '*.ts',
            directory: '/src'
          },
          result: {
            files: [
              '/src/main.ts',
              '/src/app.ts',
              '/src/services/api.ts',
              '/src/utils/helpers.ts'
            ],
            count: 4
          }
        },
        {
          name: 'respond_to_user',
          arguments: {
            message: 'I found 4 TypeScript files in your /src directory:\n- main.ts\n- app.ts\n- services/api.ts\n- utils/helpers.ts'
          }
        }
      ]
    },
    {
      id: '5',
      role: 'assistant',
      content: 'Regular message without tool calls',
      timestamp: new Date()
    }
  ];
}