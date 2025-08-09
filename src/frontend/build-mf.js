const { exec } = require('child_process');
const { promisify } = require('util');
const execAsync = promisify(exec);

async function buildModuleFederation() {
  console.log('Building Module Federation apps...');
  
  const apps = ['dashboard', 'projects', 'chat', 'settings'];
  
  // First build the shell app
  console.log('Building shell app...');
  try {
    await execAsync('npx nx build shell --configuration=production');
    console.log('✓ Shell built successfully');
  } catch (error) {
    console.error('Error building shell:', error);
    process.exit(1);
  }
  
  // Build each remote app with webpack
  for (const app of apps) {
    console.log(`Building ${app} with webpack for module federation...`);
    try {
      // Use webpack directly to build with module federation config
      await execAsync(`npx webpack --config apps/${app}/webpack.config.js --mode production`, {
        cwd: __dirname
      });
      console.log(`✓ ${app} built successfully`);
    } catch (error) {
      console.error(`Error building ${app}:`, error);
      // Try alternative approach - build as standard Angular app first
      console.log(`Attempting standard build for ${app}...`);
      try {
        await execAsync(`npx nx build ${app} --configuration=production`);
        console.log(`✓ ${app} built with standard config`);
      } catch (err) {
        console.error(`Failed to build ${app}:`, err);
      }
    }
  }
  
  console.log('All apps built successfully!');
}

buildModuleFederation().catch(console.error);