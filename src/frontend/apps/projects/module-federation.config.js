module.exports = {
  name: 'projects',
  exposes: {
    './Module': './projects/src/remote-entry.ts',
  },
};
