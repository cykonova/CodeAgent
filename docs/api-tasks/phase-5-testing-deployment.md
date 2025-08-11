# Phase 5: Testing & Deployment
**Duration**: Week 7-8  
**Priority**: HIGH  
**Dependencies**: Phase 4 (API Quality)

## Overview
Establish comprehensive testing strategy and deployment pipeline to ensure code quality, reliability, and seamless releases. This phase creates the foundation for continuous delivery and production operations.

## Tasks

### 5.1 Comprehensive Unit & Integration Tests (3 days)
**Owner**: Backend Team  
**Complexity**: High

#### Requirements
- Achieve 80% code coverage minimum
- Create test data builders
- Implement test fixtures
- Add performance tests
- Create contract tests

#### Test Project Structure
```
src/
├── CodeAgent.Gateway.Tests/
│   ├── Unit/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Services/
│   │   └── Validators/
│   ├── Integration/
│   │   ├── ApiTests/
│   │   ├── WebSocketTests/
│   │   └── DatabaseTests/
│   ├── Fixtures/
│   │   ├── DatabaseFixture.cs
│   │   ├── WebApplicationFixture.cs
│   │   └── TestDataBuilder.cs
│   └── TestHelpers/
```

#### Unit Test Examples
```csharp
public class AgentServiceTests
{
    private readonly Mock<IAgentRepository> _repositoryMock;
    private readonly Mock<ILogger<AgentService>> _loggerMock;
    private readonly Mock<IProviderRegistry> _providerMock;
    private readonly AgentService _sut;

    public AgentServiceTests()
    {
        _repositoryMock = new Mock<IAgentRepository>();
        _loggerMock = new Mock<ILogger<AgentService>>();
        _providerMock = new Mock<IProviderRegistry>();
        _sut = new AgentService(_repositoryMock.Object, _loggerMock.Object, _providerMock.Object);
    }

    [Fact]
    public async Task CreateAgent_ValidRequest_ReturnsCreatedAgent()
    {
        // Arrange
        var request = new AgentBuilder()
            .WithName("Test Agent")
            .WithProvider("anthropic")
            .Build();

        _providerMock.Setup(x => x.IsProviderAvailable("anthropic"))
            .ReturnsAsync(true);

        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) => a);

        // Act
        var result = await _sut.CreateAgentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Agent");
        _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Agent>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("a")] // Too short
    public async Task CreateAgent_InvalidName_ThrowsValidationException(string name)
    {
        // Arrange
        var request = new AgentBuilder().WithName(name).Build();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => 
            _sut.CreateAgentAsync(request));
    }
}
```

#### Integration Test Setup
```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove real database
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                
                // Add test database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Override external services
                services.AddSingleton<IProviderService, MockProviderService>();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAgents_ReturnsSuccessAndCorrectContentType()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/agents");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.ToString()
            .Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task CreateAgent_EndToEnd_WorksCorrectly()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new
        {
            name = "Integration Test Agent",
            provider = "mock",
            model = "test-model"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/agents", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        
        var content = await response.Content.ReadFromJsonAsync<AgentResponse>();
        content.Id.Should().NotBeEmpty();
        content.Name.Should().Be(request.name);
    }
}
```

#### WebSocket Integration Tests
```csharp
public class WebSocketIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private WebSocketClient _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.WithWebHostBuilder(builder => { })
            .CreateWebSocketClient();
    }

    [Fact]
    public async Task WebSocket_Connect_ReceivesWelcomeMessage()
    {
        // Arrange
        var uri = new Uri("ws://localhost/ws");

        // Act
        var webSocket = await _client.ConnectAsync(uri, CancellationToken.None);
        var buffer = new ArraySegment<byte>(new byte[4096]);
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

        // Assert
        result.MessageType.Should().Be(WebSocketMessageType.Text);
        var message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
        message.Should().Contain("welcome");
    }

    [Fact]
    public async Task WebSocket_SendMessage_ReceivesResponse()
    {
        // Arrange
        var webSocket = await ConnectAsync();
        var message = JsonSerializer.Serialize(new { type = "ping" });

        // Act
        await SendMessageAsync(webSocket, message);
        var response = await ReceiveMessageAsync(webSocket);

        // Assert
        response.Should().Contain("pong");
    }
}
```

#### Test Data Builders
```csharp
public class AgentBuilder
{
    private string _name = "Default Agent";
    private string _provider = "anthropic";
    private string _model = "claude-3";
    private Dictionary<string, object> _config = new();

    public AgentBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AgentBuilder WithProvider(string provider)
    {
        _provider = provider;
        return this;
    }

    public AgentBuilder WithConfig(string key, object value)
    {
        _config[key] = value;
        return this;
    }

    public CreateAgentRequest Build()
    {
        return new CreateAgentRequest
        {
            Name = _name,
            Provider = _provider,
            Model = _model,
            Configuration = _config
        };
    }
}
```

#### Test Coverage Requirements
| Component | Minimum Coverage | Target Coverage |
|-----------|-----------------|-----------------|
| Controllers | 80% | 90% |
| Services | 85% | 95% |
| Validators | 90% | 100% |
| Middleware | 75% | 85% |
| Repositories | 80% | 90% |

#### Acceptance Criteria
- [ ] 80% overall code coverage
- [ ] All critical paths tested
- [ ] Integration tests for all APIs
- [ ] WebSocket tests implemented
- [ ] Test data builders created
- [ ] Performance tests added

### 5.2 Docker Containerization (2 days)
**Owner**: DevOps Team  
**Complexity**: Medium

#### Requirements
- Create multi-stage Dockerfiles
- Optimize image sizes
- Implement health checks
- Configure security scanning
- Create docker-compose setup

#### Multi-Stage Dockerfile
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/CodeAgent.Gateway/CodeAgent.Gateway.csproj", "CodeAgent.Gateway/"]
COPY ["src/CodeAgent.Core/CodeAgent.Core.csproj", "CodeAgent.Core/"]
COPY ["src/CodeAgent.Providers/CodeAgent.Providers.csproj", "CodeAgent.Providers/"]
RUN dotnet restore "CodeAgent.Gateway/CodeAgent.Gateway.csproj"

# Copy everything and build
COPY src/ .
WORKDIR "/src/CodeAgent.Gateway"
RUN dotnet build "CodeAgent.Gateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodeAgent.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Security: Run as non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Install health check dependencies
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Security hardening
RUN chown -R appuser:appuser /app
USER appuser

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 5000

ENTRYPOINT ["dotnet", "CodeAgent.Gateway.dll"]
```

#### Docker Compose Configuration
```yaml
version: '3.8'

services:
  gateway:
    build:
      context: .
      dockerfile: src/CodeAgent.Gateway/Dockerfile
    image: codeagent/gateway:${VERSION:-latest}
    container_name: codeagent-gateway
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=${ENVIRONMENT:-Production}
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - Redis__ConnectionString=${REDIS_CONNECTION}
      - Authentication__Jwt__SecretKey=${JWT_SECRET}
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy
    networks:
      - codeagent-network
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  db:
    image: postgres:15-alpine
    container_name: codeagent-db
    environment:
      - POSTGRES_DB=codeagent
      - POSTGRES_USER=${DB_USER}
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "5432:5432"
    networks:
      - codeagent-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: codeagent-redis
    command: redis-server --appendonly yes --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis-data:/data
    ports:
      - "6379:6379"
    networks:
      - codeagent-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  nginx:
    image: nginx:alpine
    container_name: codeagent-nginx
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - gateway
    networks:
      - codeagent-network

networks:
  codeagent-network:
    driver: bridge

volumes:
  postgres-data:
  redis-data:
```

#### Security Scanning
```yaml
# .github/workflows/docker-security.yml
name: Docker Security Scan

on:
  push:
    paths:
      - '**/Dockerfile'

jobs:
  scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Run Trivy vulnerability scanner
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: 'codeagent/gateway:latest'
          format: 'sarif'
          output: 'trivy-results.sarif'
          severity: 'CRITICAL,HIGH'
      
      - name: Upload Trivy results
        uses: github/codeql-action/upload-sarif@v2
        with:
          sarif_file: 'trivy-results.sarif'
```

#### Acceptance Criteria
- [ ] Multi-stage Dockerfile created
- [ ] Image size < 200MB
- [ ] Health checks working
- [ ] Security scanning passed
- [ ] Docker-compose tested
- [ ] Non-root user configured

### 5.3 CI/CD Pipeline Setup (2 days)
**Owner**: DevOps Team  
**Complexity**: Medium

#### Requirements
- Set up GitHub Actions
- Implement build pipeline
- Add test automation
- Configure deployments
- Add rollback capability

#### GitHub Actions Workflow
```yaml
# .github/workflows/ci-cd.yml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  release:
    types: [created]

env:
  DOTNET_VERSION: '8.0.x'
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    timeout-minutes: 10
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Cache NuGet packages
      uses: actions/cache@v3
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
        restore-keys: |
          ${{ runner.os }}-nuget-
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Run unit tests
      run: |
        dotnet test --no-build --configuration Release \
          --logger "trx;LogFileName=test-results.trx" \
          --collect:"XPlat Code Coverage" \
          --results-directory ./TestResults
    
    - name: Generate code coverage report
      uses: danielpalme/ReportGenerator-GitHub-Action@5
      with:
        reports: 'TestResults/**/coverage.cobertura.xml'
        targetdir: 'CoverageReport'
        reporttypes: 'HtmlInline;Cobertura'
    
    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: ./CoverageReport/Cobertura.xml
        flags: unittests
        name: codecov-umbrella
    
    - name: Check code coverage threshold
      run: |
        coverage=$(grep -oP 'line-rate="\K[0-9.]+' ./CoverageReport/Cobertura.xml)
        if (( $(echo "$coverage < 0.80" | bc -l) )); then
          echo "Code coverage is below 80%: $coverage"
          exit 1
        fi
    
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: TestResults/

  integration-tests:
    needs: build-and-test
    runs-on: ubuntu-latest
    timeout-minutes: 15
    
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: testpass
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
      
      redis:
        image: redis:7
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 6379:6379
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Run integration tests
      env:
        ConnectionStrings__DefaultConnection: "Host=localhost;Database=testdb;Username=postgres;Password=testpass"
        Redis__ConnectionString: "localhost:6379"
      run: |
        dotnet test tests/Integration \
          --configuration Release \
          --logger "trx;LogFileName=integration-results.trx"

  docker-build:
    needs: [build-and-test, integration-tests]
    runs-on: ubuntu-latest
    if: github.event_name != 'pull_request'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v2
    
    - name: Log in to GitHub Container Registry
      uses: docker/login-action@v2
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v4
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=ref,event=pr
          type=semver,pattern={{version}}
          type=semver,pattern={{major}}.{{minor}}
          type=sha
    
    - name: Build and push Docker image
      uses: docker/build-push-action@v4
      with:
        context: .
        file: ./src/CodeAgent.Gateway/Dockerfile
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max
    
    - name: Run Trivy vulnerability scanner
      uses: aquasecurity/trivy-action@master
      with:
        image-ref: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ steps.meta.outputs.version }}
        format: 'sarif'
        output: 'trivy-results.sarif'
    
    - name: Upload Trivy results
      uses: github/codeql-action/upload-sarif@v2
      with:
        sarif_file: 'trivy-results.sarif'

  deploy-staging:
    needs: docker-build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/develop'
    environment:
      name: staging
      url: https://staging.codeagent.com
    
    steps:
    - name: Deploy to Kubernetes
      uses: azure/k8s-deploy@v4
      with:
        manifests: |
          k8s/staging/
        images: |
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
        imagepullsecrets: |
          github-registry

  deploy-production:
    needs: docker-build
    runs-on: ubuntu-latest
    if: github.event_name == 'release'
    environment:
      name: production
      url: https://api.codeagent.com
    
    steps:
    - name: Deploy to Production
      uses: azure/k8s-deploy@v4
      with:
        manifests: |
          k8s/production/
        images: |
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.event.release.tag_name }}
        imagepullsecrets: |
          github-registry
        strategy: blue-green
        route-method: service
```

#### Deployment Strategies
```yaml
# k8s/production/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: codeagent-gateway
  labels:
    app: codeagent-gateway
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: codeagent-gateway
  template:
    metadata:
      labels:
        app: codeagent-gateway
    spec:
      containers:
      - name: gateway
        image: ghcr.io/codeagent/gateway:latest
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
        resources:
          limits:
            cpu: "1"
            memory: "512Mi"
          requests:
            cpu: "250m"
            memory: "256Mi"
```

#### Acceptance Criteria
- [ ] GitHub Actions configured
- [ ] Build pipeline working
- [ ] Tests automated
- [ ] Docker images built
- [ ] Deployments configured
- [ ] Rollback tested

### 5.4 Load Testing Suite (2 days)
**Owner**: QA Team  
**Complexity**: Medium

#### Requirements
- Create load test scenarios
- Implement stress tests
- Add spike testing
- Configure soak tests
- Generate performance reports

#### K6 Load Test Scripts
```javascript
// load-tests/api-load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '2m', target: 100 }, // Ramp up
    { duration: '5m', target: 100 }, // Stay at 100 users
    { duration: '2m', target: 200 }, // Ramp up
    { duration: '5m', target: 200 }, // Stay at 200 users
    { duration: '2m', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'], // 95% of requests under 1s
    errors: ['rate<0.01'],              // Error rate under 1%
    http_req_failed: ['rate<0.05'],     // Failed requests under 5%
  },
};

const BASE_URL = __ENV.API_URL || 'https://staging.codeagent.com';

export function setup() {
  // Login and get token
  const loginRes = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify({
    email: 'test@example.com',
    password: 'TestPassword123!'
  }), {
    headers: { 'Content-Type': 'application/json' },
  });
  
  const token = JSON.parse(loginRes.body).token;
  return { token };
}

export default function (data) {
  const headers = {
    'Authorization': `Bearer ${data.token}`,
    'Content-Type': 'application/json',
  };

  // Scenario 1: Get agents list
  const agentsRes = http.get(`${BASE_URL}/api/v1/agents`, { headers });
  check(agentsRes, {
    'agents status is 200': (r) => r.status === 200,
    'agents response time < 500ms': (r) => r.timings.duration < 500,
  });
  errorRate.add(agentsRes.status !== 200);

  sleep(1);

  // Scenario 2: Create agent
  const createRes = http.post(
    `${BASE_URL}/api/v1/agents`,
    JSON.stringify({
      name: `Agent-${Date.now()}`,
      provider: 'anthropic',
      model: 'claude-3',
    }),
    { headers }
  );
  check(createRes, {
    'create status is 201': (r) => r.status === 201,
    'create response has id': (r) => JSON.parse(r.body).id !== undefined,
  });
  errorRate.add(createRes.status !== 201);

  sleep(2);

  // Scenario 3: Execute workflow
  if (createRes.status === 201) {
    const agentId = JSON.parse(createRes.body).id;
    const executeRes = http.post(
      `${BASE_URL}/api/v1/agents/${agentId}/execute`,
      JSON.stringify({
        input: 'Test input',
        parameters: { temperature: 0.7 },
      }),
      { headers }
    );
    check(executeRes, {
      'execute status is 202': (r) => r.status === 202,
      'execute response time < 2000ms': (r) => r.timings.duration < 2000,
    });
    errorRate.add(executeRes.status !== 202);
  }

  sleep(1);
}

export function teardown(data) {
  // Cleanup if needed
}
```

#### Stress Test Configuration
```javascript
// load-tests/stress-test.js
export const options = {
  stages: [
    { duration: '5m', target: 500 },  // Ramp up to 500 users
    { duration: '10m', target: 500 }, // Stay at 500
    { duration: '5m', target: 1000 }, // Ramp up to 1000
    { duration: '10m', target: 1000 }, // Stay at 1000
    { duration: '5m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(99)<3000'], // 99% under 3s even under stress
    http_req_failed: ['rate<0.1'],     // Less than 10% failure rate
  },
};
```

#### Performance Test Matrix
| Test Type | Duration | Users | Expected RPS | Success Criteria |
|-----------|----------|-------|--------------|------------------|
| Smoke | 5 min | 5 | 10 | All pass |
| Load | 30 min | 200 | 500 | 95% < 1s |
| Stress | 30 min | 1000 | 2000 | No crashes |
| Spike | 10 min | 0→500→0 | Variable | Recovery < 30s |
| Soak | 4 hours | 100 | 200 | No memory leaks |

#### Acceptance Criteria
- [ ] Load test scenarios created
- [ ] Stress tests implemented
- [ ] Spike tests configured
- [ ] Soak tests validated
- [ ] Performance reports generated
- [ ] Bottlenecks identified

### 5.5 Infrastructure as Code (2 days)
**Owner**: DevOps Team  
**Complexity**: High

#### Requirements
- Create Terraform modules
- Configure cloud resources
- Set up monitoring
- Implement auto-scaling
- Document deployment

#### Terraform Configuration
```hcl
# infrastructure/main.tf
terraform {
  required_version = ">= 1.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
  }
  
  backend "azurerm" {
    resource_group_name  = "terraform-state-rg"
    storage_account_name = "tfstatecodeagent"
    container_name      = "tfstate"
    key                 = "prod.terraform.tfstate"
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${var.project}-${var.environment}-rg"
  location = var.location
  
  tags = {
    Environment = var.environment
    Project     = var.project
    ManagedBy   = "Terraform"
  }
}

# AKS Cluster
resource "azurerm_kubernetes_cluster" "main" {
  name                = "${var.project}-${var.environment}-aks"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  dns_prefix          = "${var.project}-${var.environment}"
  
  default_node_pool {
    name                = "default"
    node_count          = var.node_count
    vm_size            = var.vm_size
    enable_auto_scaling = true
    min_count          = var.min_nodes
    max_count          = var.max_nodes
  }
  
  identity {
    type = "SystemAssigned"
  }
  
  network_profile {
    network_plugin    = "azure"
    load_balancer_sku = "standard"
  }
  
  addon_profile {
    oms_agent {
      enabled                    = true
      log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
    }
  }
}

# PostgreSQL
resource "azurerm_postgresql_flexible_server" "main" {
  name                = "${var.project}-${var.environment}-pg"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  
  sku_name   = var.pg_sku
  version    = "15"
  storage_mb = var.pg_storage_mb
  
  administrator_login    = var.db_admin_username
  administrator_password = var.db_admin_password
  
  backup_retention_days        = 30
  geo_redundant_backup_enabled = var.environment == "production"
  
  high_availability {
    mode                      = "ZoneRedundant"
    standby_availability_zone = "2"
  }
}

# Redis Cache
resource "azurerm_redis_cache" "main" {
  name                = "${var.project}-${var.environment}-redis"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  
  capacity            = var.redis_capacity
  family              = var.redis_family
  sku_name           = var.redis_sku
  
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
  
  redis_configuration {
    enable_authentication = true
    maxmemory_policy     = "allkeys-lru"
  }
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "${var.project}-${var.environment}-ai"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"
  
  retention_in_days = var.environment == "production" ? 90 : 30
}
```

#### Kubernetes Manifests
```yaml
# k8s/base/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
  - namespace.yaml
  - deployment.yaml
  - service.yaml
  - ingress.yaml
  - configmap.yaml
  - hpa.yaml

commonLabels:
  app: codeagent
  version: v1

configMapGenerator:
  - name: app-config
    literals:
      - environment=production
      - log_level=info

secretGenerator:
  - name: app-secrets
    literals:
      - jwt_secret=${JWT_SECRET}
      - db_password=${DB_PASSWORD}
```

#### Acceptance Criteria
- [ ] Terraform modules created
- [ ] Resources provisioned
- [ ] Auto-scaling configured
- [ ] Monitoring integrated
- [ ] Documentation complete
- [ ] Disaster recovery plan

## Testing Requirements

### Deployment Testing
- Blue-green deployment
- Canary deployment
- Rollback procedures
- Zero-downtime deployment

### Performance Benchmarks
- API response time < 200ms (p50)
- Throughput > 1000 RPS
- Error rate < 0.1%
- Availability > 99.9%

## Success Metrics
- Deployment frequency: Daily
- Lead time: < 1 hour
- MTTR: < 30 minutes
- Change failure rate: < 5%
- Test coverage: > 80%

## Dependencies
- Container registry access
- Kubernetes cluster
- Cloud provider account
- Monitoring tools setup
- Load testing tools

## Deliverables
- [ ] Complete test suite (80% coverage)
- [ ] Docker images and compose files
- [ ] CI/CD pipeline configuration
- [ ] Load test scenarios and reports
- [ ] Infrastructure as Code templates
- [ ] Deployment documentation
- [ ] Runbooks for operations
- [ ] Performance baseline report

## Notes
- Consider GitOps with ArgoCD for deployments
- Evaluate service mesh (Istio/Linkerd) for advanced traffic management
- Plan for multi-region deployment in future
- Consider chaos engineering tools (Chaos Monkey)