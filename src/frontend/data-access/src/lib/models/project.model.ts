export interface Project {
  id: string;
  name: string;
  description?: string;
  path: string;
  type: ProjectType;
  status: ProjectStatus;
  configuration: ProjectConfiguration;
  agents: string[];
  workflows: WorkflowTemplate[];
  createdAt: Date;
  updatedAt: Date;
  lastAccessedAt?: Date;
}

export enum ProjectType {
  WebApplication = 'web-application',
  API = 'api',
  Library = 'library',
  Microservice = 'microservice',
  Mobile = 'mobile',
  Desktop = 'desktop',
  CLI = 'cli',
  Other = 'other'
}

export enum ProjectStatus {
  Active = 'active',
  Inactive = 'inactive',
  Archived = 'archived',
  Error = 'error'
}

export interface ProjectConfiguration {
  language?: string;
  framework?: string;
  buildCommand?: string;
  testCommand?: string;
  startCommand?: string;
  environmentVariables?: Record<string, string>;
  dockerConfig?: DockerConfiguration;
  gitConfig?: GitConfiguration;
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
  path: string;
  type: ProjectType;
  configuration?: ProjectConfiguration;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
  status?: ProjectStatus;
  configuration?: ProjectConfiguration;
}