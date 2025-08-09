export const environment = {
  production: true,
  apiUrl: '/api',
  wsUrl: `wss://${window.location.hostname}/ws`,
  version: '1.0.0',
  features: {
    authentication: true,
    multiLanguage: true,
    darkMode: true,
    analytics: true,
    errorReporting: true
  },
  auth: {
    tokenRefreshInterval: 600000, // 10 minutes
    sessionTimeout: 3600000, // 1 hour
    rememberMeDuration: 2592000000 // 30 days
  },
  retry: {
    maxRetries: 5,
    retryDelay: 2000,
    retryBackoff: 2
  },
  cache: {
    maxAge: 600000, // 10 minutes
    maxSize: 200 // Maximum number of cached items
  }
};