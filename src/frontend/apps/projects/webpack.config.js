const { withModuleFederation } = require('@angular-architects/module-federation/webpack');
const config = require('./module-federation.config');

module.exports = withModuleFederation(config);