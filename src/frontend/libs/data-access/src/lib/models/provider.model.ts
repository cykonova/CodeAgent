export interface Provider {
  id: string;
  name: string;
  type: ProviderType;
  enabled: boolean;
  configuration: ProviderConfiguration;
  status: ProviderStatus;
  models: ModelInfo[];
  createdAt?: Date;
  updatedAt?: Date;
}

export enum ProviderType {
  Anthropic = 'anthropic',
  OpenAI = 'openai',
  Ollama = 'ollama',
  Custom = 'custom'
}

export interface ProviderConfiguration {
  apiKey?: string;
  baseUrl?: string;
  model?: string;
  maxTokens?: number;
  temperature?: number;
  timeout?: number;
  customHeaders?: Record<string, string>;
}

export interface ProviderStatus {
  isConnected: boolean;
  lastChecked?: Date;
  error?: string;
  latency?: number;
}

export interface ModelInfo {
  id: string;
  name: string;
  description?: string;
  contextWindow?: number;
  maxOutputTokens?: number;
  supportsFunctions?: boolean;
  supportsVision?: boolean;
}

export interface CreateProviderRequest {
  name: string;
  type: ProviderType;
  configuration: ProviderConfiguration;
}

export interface UpdateProviderRequest {
  name?: string;
  enabled?: boolean;
  configuration?: ProviderConfiguration;
}