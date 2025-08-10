module.exports = {
  name: 'projects',
  exposes: {
    './Module': './apps/projects/src/remote-entry.ts',
  },
};
