export interface Agent {
  id: string;
  name: string;
  type: AgentType;
  description?: string;
  providerId: string;
  configuration: AgentConfiguration;
  capabilities: string[];
  status: AgentStatus;
  metrics?: AgentMetrics;
  createdAt: Date;
  updatedAt: Date;
}

export enum AgentType {
  CodeGenerator = 'code-generator',
  CodeReviewer = 'code-reviewer',
  TestGenerator = 'test-generator',
  Documentation = 'documentation',
  Debugger = 'debugger',
  Refactoring = 'refactoring',
  Security = 'security',
  Performance = 'performance',
  Custom = 'custom'
}

export enum AgentStatus {
  Idle = 'idle',
  Running = 'running',
  Paused = 'paused',
  Error = 'error',
  Offline = 'offline'
}

export interface AgentConfiguration {
  systemPrompt?: string;
  temperature?: number;
  maxTokens?: number;
  tools?: string[];
  memory?: boolean;
  streaming?: boolean;
  customParameters?: Record<string, any>;
}

export interface AgentMetrics {
  totalRuns: number;
  successfulRuns: number;
  failedRuns: number;
  averageResponseTime: number;
  tokenUsage: TokenUsage;
  lastRunAt?: Date;
}

export interface TokenUsage {
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  cost?: number;
}

export interface AgentExecution {
  id: string;
  agentId: string;
  projectId?: string;
  status: ExecutionStatus;
  input: string;
  output?: string;
  error?: string;
  startedAt: Date;
  completedAt?: Date;
  duration?: number;
  tokenUsage?: TokenUsage;
}

export enum ExecutionStatus {
  Pending = 'pending',
  Running = 'running',
  Completed = 'completed',
  Failed = 'failed',
  Cancelled = 'cancelled'
}

export interface CreateAgentRequest {
  name: string;
  type: AgentType;
  description?: string;
  providerId: string;
  configuration: AgentConfiguration;
}

export interface UpdateAgentRequest {
  name?: string;
  description?: string;
  configuration?: AgentConfiguration;
  status?: AgentStatus;
}