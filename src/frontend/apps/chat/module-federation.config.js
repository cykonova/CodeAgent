module.exports = {
  name: 'chat',
  exposes: {
    './Module': './apps/chat/src/app/app.ts',
  },
};