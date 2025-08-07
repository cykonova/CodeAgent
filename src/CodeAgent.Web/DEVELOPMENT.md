# CodeAgent Web Development Guide

## Quick Start

### Recommended Method (Automated)
```bash
cd src/CodeAgent.Web
./start-dev.sh    # macOS/Linux
# or
start-dev.cmd     # Windows
```

### Manual Method (Two Terminals)
```bash
# Terminal 1 - Angular Dev Server
cd src/CodeAgent.Web/client
npm start

# Terminal 2 - .NET API Server  
cd src/CodeAgent.Web
dotnet run
```

Access the application at: **http://localhost:5001**

## Architecture

- **Development**: .NET server (5001) ↔ proxy ↔ Angular dev server (4200)
- **Production**: .NET server (5001) serves pre-built files from wwwroot

## Build for Production

```bash
cd src/CodeAgent.Web/client
npm run build:prod    # Outputs to ../wwwroot

cd ..
dotnet run           # Serves from wwwroot
```

## Troubleshooting

### "Angular CLI process did not start" Error
**Fixed**: Changed from auto-starting Angular CLI to proxy mode. Now Angular must be started manually first.

### "Would you like to enable autocompletion?" Prompt
**Fixed**: Disabled Angular CLI analytics and configured completion prompts to prevent scripts from hanging on interactive prompts.

### "Could not read package.json" Error  
**Solution**: Make sure you're running commands from the correct directories:
- Angular commands: `src/CodeAgent.Web/client/`
- .NET commands: `src/CodeAgent.Web/`

### "Couldn't find a project to run"
**Solution**: Run `dotnet run` from `src/CodeAgent.Web/` directory where `CodeAgent.Web.csproj` is located.

## Key Files

- `src/CodeAgent.Web/Program.cs` - .NET server configuration
- `src/CodeAgent.Web/client/angular.json` - Angular build configuration  
- `src/CodeAgent.Web/client/package.json` - npm scripts
- `src/CodeAgent.Web/start-dev.*` - Development startup scripts

## Development Workflow

1. Start Angular dev server (localhost:4200)
2. Start .NET server (localhost:5001) 
3. .NET proxies requests to Angular in development
4. Access application via .NET server URL for full functionality
5. Angular hot-reload works through the proxy

## Production Deployment

1. Build Angular: `npm run build:prod` → outputs to `wwwroot`
2. Run .NET: `dotnet run` → serves static files from `wwwroot`
3. Single server deployment with pre-built assets