# CodeAgent Web Client

This is the Angular frontend for the CodeAgent Web application.

## Development

The CodeAgent Web project serves this Angular client in two modes:

### Development Mode
1. **Option 1 - Use development script (Recommended):**
   ```bash
   # From the CodeAgent.Web directory
   ./start-dev.sh    # On macOS/Linux
   start-dev.cmd     # On Windows
   ```

2. **Option 2 - Manual startup:**
   ```bash
   # Terminal 1: Start Angular dev server
   cd src/CodeAgent.Web/client
   npm start

   # Terminal 2: Start .NET server (from project root)
   cd src/CodeAgent.Web
   dotnet run
   ```

   Navigate to `http://localhost:5001/` - the .NET server will proxy requests to Angular at `localhost:4200`.

### Production Mode
```bash
# Build Angular for production (outputs to ../wwwroot)
cd client
npm run build:prod

# Start .NET server (serves from wwwroot)
cd ..
dotnet run
```

## Available Scripts

- `npm start` - Start Angular dev server on localhost:4200
- `npm run build` - Build for development
- `npm run build:prod` - Build for production (outputs to ../wwwroot)
- `npm test` - Run unit tests
- `npm run watch` - Build in watch mode

## Architecture

- **Development**: Angular CLI dev server (localhost:4200) ← proxied by ← .NET server (localhost:5001)
- **Production**: .NET server serves pre-built Angular files from wwwroot

## Features

- Real-time chat with SignalR integration
- File browser and editor
- Provider configuration (OpenAI, Claude, Ollama)
- Material Design UI with dark/light theme
- Session persistence
- Security features (CSRF protection, input validation)

## Original Angular CLI Documentation

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.1.5.

### Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

### Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

### Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.