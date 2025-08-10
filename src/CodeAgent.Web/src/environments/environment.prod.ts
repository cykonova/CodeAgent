// Production environment configuration
export const environment = {
  production: true,
  apiUrl: '/api',
  wsUrl: 'wss://' + window.location.hostname + '/ws',
  features: {
    enableDebug: false,
    enableLogging: false,
    enableAnalytics: true
  },
  debug: {
    logLevel: 'error',
    enableProfiler: false
  }
};