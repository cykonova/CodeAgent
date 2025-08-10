export interface Project {
  id: string;
  name: string;
  description?: string;
  type: ProjectType;
  templateName?: string;
  createdAt: Date;
  updatedAt: Date;
  configuration: ProjectConfiguration;
  state: ProjectState;
  metadata: Record<string, any>;
}

export enum ProjectType {
  Standard = 'standard',
  Fast = 'fast',
  Quality = 'quality',
  Budget = 'budget',
  Custom = 'custom'
}

export enum ProjectStatus {
  Idle = 'idle',
  Running = 'running',
  Paused = 'paused',
  Completed = 'completed',
  Failed = 'failed',
  Cancelled = 'cancelled'
}

export interface ProjectState {
  status: ProjectStatus;
  currentStage?: string;
  lastRunAt?: Date;
  lastRunDuration?: string;
  runHistory: ProjectRun[];
  costSummary: CostSummary;
  runtimeData: Record<string, any>;
}

export interface ProjectRun {
  id: string;
  startedAt: Date;
  completedAt?: Date;
  status: ProjectStatus;
  errorMessage?: string;
  stageResults: StageResult[];
  cost: RunCost;
}

export interface StageResult {
  stageName: string;
  startedAt: Date;
  completedAt?: Date;
  status: StageStatus;
  agentId?: string;
  output: Record<string, any>;
  errorMessage?: string;
}

export enum StageStatus {
  Pending = 'pending',
  Running = 'running',
  Completed = 'completed',
  Failed = 'failed',
  Skipped = 'skipped'
}

export interface CostSummary {
  totalCost: number;
  totalTokens: number;
  todayCost: number;
  todayTokens: number;
  monthCost: number;
  monthTokens: number;
  lastUpdated: Date;
}

export interface RunCost {
  totalCost: number;
  inputTokens: number;
  outputTokens: number;
  providerCosts: Record<string, ProviderCost>;
}

export interface ProviderCost {
  providerId: string;
  model: string;
  inputTokens: number;
  outputTokens: number;
  cost: number;
}

export interface ProjectConfiguration {
  workingDirectory?: string;
  language?: string;
  framework?: string;
  buildCommand?: string;
  testCommand?: string;
  startCommand?: string;
  environmentVariables?: Record<string, string>;
  dockerConfig?: DockerConfiguration;
  gitConfig?: GitConfiguration;
  agents?: ProjectAgentConfiguration[];
}

export interface DockerConfiguration {
  imageName?: string;
  dockerfile?: string;
  ports?: number[];
  volumes?: string[];
  environment?: Record<string, string>;
}

export interface GitConfiguration {
  repository?: string;
  branch?: string;
  autoCommit?: boolean;
  commitMessage?: string;
}

export interface ProjectAgentConfiguration {
  id: string;
  name: string;
  type: string;
  enabled: boolean;
  configuration?: Record<string, any>;
}

export interface WorkflowTemplate {
  id: string;
  name: string;
  description?: string;
  steps: WorkflowStep[];
  triggers?: WorkflowTrigger[];
}

export interface WorkflowStep {
  id: string;
  name: string;
  type: string;
  agentId?: string;
  action: string;
  parameters?: Record<string, any>;
  conditions?: WorkflowCondition[];
}

export interface WorkflowTrigger {
  type: 'manual' | 'schedule' | 'event' | 'webhook';
  configuration?: Record<string, any>;
}

export interface WorkflowCondition {
  field: string;
  operator: 'equals' | 'contains' | 'greater' | 'less' | 'regex';
  value: any;
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
  type: ProjectType;
  templateName?: string;
  configuration?: ProjectConfiguration;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
  configuration?: ProjectConfiguration;
}