export const environment = {
  production: true,
  apiUrl: '/api',
  wsUrl: `wss://${window.location.host}/ws`,
  appName: 'Code Agent Portal',
  version: '1.0.0',
  features: {
    darkMode: true,
    multiProject: true,
    analytics: true,
    debugging: false
  },
  auth: {
    tokenKey: 'code-agent-token',
    refreshTokenKey: 'code-agent-refresh-token',
    tokenExpiry: 3600, // 1 hour in seconds
    refreshTokenExpiry: 604800 // 7 days in seconds
  },
  logging: {
    level: 'error',
    enableConsole: false,
    enableRemote: true
  },
  defaults: {
    pageSize: 25,
    timeout: 30000, // 30 seconds
    retryAttempts: 3,
    retryDelay: 1000 // 1 second
  }
};