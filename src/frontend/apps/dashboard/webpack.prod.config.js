const { withModuleFederation } = require('@angular-architects/module-federation/webpack');
const config = require('./module-federation.config');
const webpack = require('webpack');

module.exports = withModuleFederation({
  ...config,
  // Override the exposes path to be correct
  exposes: {
    './Module': './apps/dashboard/src/app/app.ts',
  },
});