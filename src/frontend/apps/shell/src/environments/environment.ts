export const environment = {
  production: false,
  apiUrl: 'http://localhost:7000/api',
  wsUrl: 'ws://localhost:7000/ws',
  version: '1.0.0-dev',
  features: {
    authentication: true,
    multiLanguage: true,
    darkMode: true,
    analytics: false,
    errorReporting: false
  },
  auth: {
    tokenRefreshInterval: 300000, // 5 minutes
    sessionTimeout: 1800000, // 30 minutes
    rememberMeDuration: 604800000 // 7 days
  },
  retry: {
    maxRetries: 3,
    retryDelay: 1000,
    retryBackoff: 2
  },
  cache: {
    maxAge: 300000, // 5 minutes
    maxSize: 100 // Maximum number of cached items
  }
};