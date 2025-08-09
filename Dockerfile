# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY src/*.sln .
COPY src/*/*.csproj ./
RUN for file in $(ls *.csproj); do \
    mkdir -p ${file%.*}/ && mv $file ${file%.*}/; \
    done

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ .

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish CodeAgent.Gateway/CodeAgent.Gateway.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install Docker CLI for sandbox management
RUN apt-get update && apt-get install -y \
    docker.io \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create directories for runtime
RUN mkdir -p /var/codeagent/workspaces && \
    chmod 755 /var/codeagent/workspaces

# Expose ports
EXPOSE 5000
EXPOSE 5001

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Set entrypoint
ENTRYPOINT ["dotnet", "CodeAgent.Gateway.dll"]