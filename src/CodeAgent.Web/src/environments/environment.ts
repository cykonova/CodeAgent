export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  wsUrl: 'ws://localhost:5000/ws',
  appName: 'Code Agent Portal',
  version: '1.0.0',
  features: {
    darkMode: true,
    multiProject: true,
    analytics: false,
    debugging: true
  },
  auth: {
    tokenKey: 'code-agent-token',
    refreshTokenKey: 'code-agent-refresh-token',
    tokenExpiry: 3600, // 1 hour in seconds
    refreshTokenExpiry: 604800 // 7 days in seconds
  },
  logging: {
    level: 'debug',
    enableConsole: true,
    enableRemote: false
  },
  defaults: {
    pageSize: 25,
    timeout: 30000, // 30 seconds
    retryAttempts: 3,
    retryDelay: 1000 // 1 second
  }
};