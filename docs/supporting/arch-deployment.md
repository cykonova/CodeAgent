# Deployment Architecture

## Container Structure
```
codeagent/
├── main (core application)
├── sandbox (project sandbox)
├── web (frontend only)
└── plugins/ (plugin containers)
```

## Docker Compose
```yaml
version: '3.8'

services:
  code-agent:
    image: codeagent/main:${VERSION:-latest}
    container_name: code-agent
    restart: unless-stopped
    ports:
      - "${WEB_PORT:-8080}:8080"
      - "${API_PORT:-8081}:8081"
      - "${WS_PORT:-8082}:8082"
    volumes:
      - data:/app/data
      - plugins:/app/plugins
      - projects:/app/projects
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - LOG_LEVEL=${LOG_LEVEL:-info}
      - ENABLE_TELEMETRY=${ENABLE_TELEMETRY:-false}
    networks:
      - codeagent-net
      - sandbox-net
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  ollama:
    image: ollama/ollama:latest
    container_name: ollama
    profiles: ["local"]
    volumes:
      - ollama-models:/root/.ollama
    networks:
      - codeagent-net

networks:
  codeagent-net:
    driver: bridge
  sandbox-net:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16

volumes:
  data:
  plugins:
  projects:
  ollama-models:
```

## CI/CD Pipeline
```yaml
name: Build and Deploy

on:
  push:
    branches: [main]
    tags: ['v*']

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0'
      - run: dotnet test --coverage

  build:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Build Docker image
        run: |
          docker build -t codeagent/main:${{ github.sha }} .
          docker tag codeagent/main:${{ github.sha }} codeagent/main:latest
      - name: Push to registry
        run: |
          docker push codeagent/main:${{ github.sha }}
          docker push codeagent/main:latest

  release:
    if: startsWith(github.ref, 'refs/tags/')
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            dist/*.tar.gz
            dist/*.zip
```

## Health Checks
```csharp
app.MapGet("/health", () => 
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        services = new
        {
            gateway = "up",
            docker = "up",
            database = "up"
        }
    });
});
```

## Monitoring
- Health endpoint
- Metrics endpoint
- Structured logging
- Optional Prometheus export