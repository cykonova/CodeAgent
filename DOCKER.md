# CodeAgent Docker Setup

This document describes how to build and run CodeAgent using Docker.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose v2.0+
- At least 4GB of available RAM for Docker

## Quick Start

### Production Mode

Build and run the production container:

```bash
# Build the container
./docker-run.sh build

# Start the container
./docker-run.sh start

# View the application
open http://localhost:5001
```

### Development Mode (with hot-reload)

For development with automatic code reloading:

```bash
# Build the development container
./docker-run.sh dev-build

# Start with hot-reload
./docker-run.sh dev

# Angular dev server: http://localhost:4200
# .NET API: http://localhost:5001
```

## Container Management

### Basic Commands

```bash
# Check container status
./docker-run.sh status

# View logs
./docker-run.sh logs

# Stop container
./docker-run.sh stop

# Restart container
./docker-run.sh restart

# Open shell in container
./docker-run.sh shell

# Complete rebuild
./docker-run.sh rebuild

# Clean up (remove containers and volumes)
./docker-run.sh clean
```

## Configuration

### API Keys

Create a `.env` file in the project root (it will be created automatically on first run):

```env
# OpenAI Configuration
OPENAI_API_KEY=your-openai-api-key-here

# Anthropic Configuration  
ANTHROPIC_API_KEY=your-anthropic-api-key-here

# Ollama Configuration (if using local Ollama)
OLLAMA_BASE_URL=http://host.docker.internal:11434

# LM Studio Configuration (if using LM Studio)
LMSTUDIO_BASE_URL=http://host.docker.internal:1234
```

### Volume Mappings

The following volumes are configured:

| Host Path | Container Path | Purpose |
|-----------|---------------|---------|
| `./test_workspace` | `/workspace` | CodeAgent project directory |
| `codeagent-data` | `/app/data` | Persistent data storage |
| `codeagent-logs` | `/app/logs` | Application logs |

### Ports

| Port | Service | Mode |
|------|---------|------|
| 5001 | .NET Web API | Production & Development |
| 4200 | Angular Dev Server | Development only |

## Architecture

### Production Container (`Dockerfile`)

Multi-stage build:
1. **Frontend Builder**: Node.js Alpine image builds Angular app
2. **Backend Builder**: .NET SDK builds the Web API
3. **Runtime**: Minimal .NET runtime image with both apps

### Development Container (`Dockerfile.dev`)

Single stage with:
- .NET SDK for hot-reload
- Node.js and Angular CLI for development
- Source code mounted as volumes
- Both servers run in watch mode

## Troubleshooting

### Container won't start

```bash
# Check Docker is running
docker info

# Check for port conflicts
lsof -i :5001
lsof -i :4200

# Clean rebuild
./docker-run.sh clean
./docker-run.sh rebuild
```

### Can't access the application

1. Ensure the container is running:
   ```bash
   ./docker-run.sh status
   ```

2. Check the logs for errors:
   ```bash
   ./docker-run.sh logs
   ```

3. Verify the health check:
   ```bash
   docker inspect codeagent-web --format='{{.State.Health.Status}}'
   ```

### Changes not reflected in development mode

The development container uses volume mounts and file watching. If changes aren't detected:

1. Restart the container:
   ```bash
   ./docker-run.sh dev-down
   ./docker-run.sh dev
   ```

2. Clear Angular cache:
   ```bash
   docker-compose -f docker-compose.dev.yml exec codeagent-dev rm -rf /app/src/CodeAgent.Web/client/.angular
   ```

### Permission issues with test_workspace

Ensure the test_workspace directory has proper permissions:

```bash
chmod -R 755 test_workspace
```

## Manual Docker Commands

If you prefer not to use the scripts:

### Production

```bash
# Build
docker-compose build

# Start
docker-compose up -d

# Stop
docker-compose down

# Logs
docker-compose logs -f
```

### Development

```bash
# Build
docker-compose -f docker-compose.dev.yml build

# Start
docker-compose -f docker-compose.dev.yml up

# Stop
docker-compose -f docker-compose.dev.yml down
```

## Notes

- The test_workspace folder is mapped to `/workspace` inside the container
- All file operations from CodeAgent will affect files in test_workspace
- API keys can be set via environment variables or .env file
- The container includes git, curl, and wget for CodeAgent operations
- Health checks run every 30 seconds to ensure the service is responding