// Development environment configuration
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  wsUrl: 'ws://localhost:5000',
  features: {
    enableDebug: true,
    enableLogging: true,
    enableAnalytics: false
  },
  debug: {
    logLevel: 'debug',
    enableProfiler: true
  }
};