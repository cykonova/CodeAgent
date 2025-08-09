export const environment = {
  production: true,
  apiUrl: 'https://staging-api.codeagent.io/api',
  wsUrl: 'wss://staging-api.codeagent.io/ws',
  version: '1.0.0-staging',
  features: {
    authentication: true,
    multiLanguage: true,
    darkMode: true,
    analytics: true,
    errorReporting: true
  },
  auth: {
    tokenRefreshInterval: 300000, // 5 minutes
    sessionTimeout: 1800000, // 30 minutes
    rememberMeDuration: 604800000 // 7 days
  },
  retry: {
    maxRetries: 4,
    retryDelay: 1500,
    retryBackoff: 2
  },
  cache: {
    maxAge: 450000, // 7.5 minutes
    maxSize: 150 // Maximum number of cached items
  }
};