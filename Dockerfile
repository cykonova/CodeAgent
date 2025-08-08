# Multi-stage build for CodeAgent Web Application
# Stage 1: Build Angular frontend
FROM node:20-alpine AS frontend-builder

WORKDIR /app/client

# Copy package files
COPY src/CodeAgent.Web/client/package*.json ./

# Install dependencies
RUN npm ci

# Copy Angular source code
COPY src/CodeAgent.Web/client/ ./

# Build Angular app
RUN npm run build:prod

# Stage 2: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-builder

WORKDIR /src

# Copy all project files
COPY src/ ./

# Restore dependencies for all projects
RUN dotnet restore CodeAgent.Web/CodeAgent.Web.csproj

# Build the web project
RUN dotnet publish CodeAgent.Web/CodeAgent.Web.csproj -c Release -o /app/publish

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install additional tools that CodeAgent might need
RUN apt-get update && apt-get install -y \
    git \
    curl \
    wget \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published .NET app
COPY --from=backend-builder /app/publish .

# Copy built Angular app to wwwroot
COPY --from=frontend-builder /app/browser ./wwwroot/browser

# Create directories for CodeAgent
RUN mkdir -p /workspace && \
    mkdir -p /app/data && \
    mkdir -p /app/logs

# Environment variables
ENV ASPNETCORE_URLS=http://+:5001 \
    ASPNETCORE_ENVIRONMENT=Development \
    CodeAgent__ProjectDirectory=/workspace \
    CodeAgent__DataDirectory=/app/data \
    CodeAgent__LogDirectory=/app/logs

# Expose ports
EXPOSE 5001

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5001/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "CodeAgent.Web.dll"]