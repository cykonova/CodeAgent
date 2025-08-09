module.exports = {
  name: 'shell',
  remotes: {
    dashboard: 'http://localhost:4201/remoteEntry.js',
    projects: 'http://localhost:4202/remoteEntry.js',
    chat: 'http://localhost:4203/remoteEntry.js',
    settings: 'http://localhost:4204/remoteEntry.js',
  },
};