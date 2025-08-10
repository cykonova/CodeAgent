module.exports = {
  name: 'chat',
  exposes: {
    './Module': './apps/chat/src/remote-entry.ts',
  },
};
