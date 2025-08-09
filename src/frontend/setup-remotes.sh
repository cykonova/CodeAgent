#!/bin/bash

# Setup remote entry files for each remote app
APPS=("dashboard" "chat" "projects" "settings")

for app in "${APPS[@]}"; do
  echo "Setting up remote entry for $app..."
  
  # Create bootstrap.ts
  cat > "$app/src/bootstrap.ts" << EOF
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig).catch((err) =>
  console.error(err)
);
EOF

  # Create remote-entry.ts
  cat > "$app/src/remote-entry.ts" << EOF
import { Routes } from '@angular/router';
import { App } from './app/app';

export const remoteRoutes: Routes = [
  {
    path: '',
    component: App
  }
];
EOF

  # Update module-federation.config.js
  cat > "apps/$app/module-federation.config.js" << EOF
module.exports = {
  name: '$app',
  exposes: {
    './Module': './$app/src/remote-entry.ts',
  },
};
EOF

  echo "✓ $app setup complete"
done

echo "All remote apps configured for module federation!"