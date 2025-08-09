module.exports = {
  name: 'chat',
  exposes: {
    './Module': './chat/src/remote-entry.ts',
  },
};
