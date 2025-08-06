# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj files and restore dependencies
COPY src/CodeAgent.Domain/*.csproj ./src/CodeAgent.Domain/
COPY src/CodeAgent.Core/*.csproj ./src/CodeAgent.Core/
COPY src/CodeAgent.Infrastructure/*.csproj ./src/CodeAgent.Infrastructure/
COPY src/CodeAgent.CLI/*.csproj ./src/CodeAgent.CLI/
COPY src/CodeAgent.Web/*.csproj ./src/CodeAgent.Web/
COPY src/CodeAgent.MCP/*.csproj ./src/CodeAgent.MCP/
COPY src/CodeAgent.Providers/CodeAgent.Providers.OpenAI/*.csproj ./src/CodeAgent.Providers/CodeAgent.Providers.OpenAI/
COPY src/CodeAgent.Providers/CodeAgent.Providers.Claude/*.csproj ./src/CodeAgent.Providers/CodeAgent.Providers.Claude/
COPY src/CodeAgent.Providers/CodeAgent.Providers.Ollama/*.csproj ./src/CodeAgent.Providers/CodeAgent.Providers.Ollama/

RUN dotnet restore src/CodeAgent.CLI/CodeAgent.CLI.csproj
RUN dotnet restore src/CodeAgent.Web/CodeAgent.Web.csproj

# Copy everything else and build
COPY . .
WORKDIR /source/src/CodeAgent.CLI
RUN dotnet publish -c Release -o /app/cli --no-restore

WORKDIR /source/src/CodeAgent.Web
RUN dotnet publish -c Release -o /app/web --no-restore

# Runtime stage for CLI
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS cli-runtime
WORKDIR /app
COPY --from=build /app/cli .
ENTRYPOINT ["dotnet", "CodeAgent.CLI.dll"]

# Runtime stage for Web
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS web-runtime
WORKDIR /app
COPY --from=build /app/web .

# Create volume for data persistence
VOLUME ["/data"]

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV CodeAgent__DataPath=/data

# Expose ports
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "CodeAgent.Web.dll"]