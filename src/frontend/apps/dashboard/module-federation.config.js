module.exports = {
  name: 'dashboard',
  exposes: {
    './Module': './apps/dashboard/src/remote-entry.ts',
  },
};
