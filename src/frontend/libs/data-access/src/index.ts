// Models
export * from './lib/models/provider.model';
export * from './lib/models/project.model';
export * from './lib/models/agent.model';

// Services  
export * from './lib/services/api.service';
export * from './lib/services/provider.service';
export * from './lib/services/project.service';
export * from './lib/services/agent.service';
export * from './lib/services/header.service';

// Chat Service (if exists)
export interface ChatMessage {
  id: string;
  content: string;
  role: 'user' | 'assistant';
  timestamp: Date;
}

export interface ChatSession {
  id: string;
  messages: ChatMessage[];
  createdAt: Date;
}

// Placeholder ChatService
export class ChatService {
  currentSession$ = null;
  agentResponses$ = null;
}
