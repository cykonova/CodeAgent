# Suggested Commands for Development

## Backend (.NET 8)
```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Start the backend server
dotnet run --project src/CodeAgent.Gateway

# Run with hot reload
dotnet watch run --project src/CodeAgent.Gateway

# Add a new project to solution
dotnet new classlib -n CodeAgent.Providers -o src/CodeAgent.Providers
dotnet sln src/CodeAgent.sln add src/CodeAgent.Providers/CodeAgent.Providers.csproj

# Add package references
dotnet add package PackageName
```

## Frontend (Angular/Nx)
```bash
# Install dependencies
npm install

# Serve development server
nx serve shell

# Build for production
nx build shell --configuration=production

# Run unit tests
nx test shell

# Run specific library tests
nx test ui-components

# Lint check
nx lint shell

# Format code
nx format:write

# Generate component
nx generate @nx/angular:component component-name
```

## Docker Operations
```bash
# Build containers
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f [service-name]

# Stop services
docker-compose down
```

## Git Commands (Darwin/macOS)
```bash
# Check status
git status

# Add files
git add .
git add <file>

# Commit
git commit -m "message"

# View logs
git log --oneline -n 10

# View diff
git diff
git diff --staged
```

## System Utilities (macOS)
```bash
# List files
ls -la

# Find files
find . -name "*.cs"

# Search in files (use ripgrep)
rg "pattern"

# Directory navigation
cd path/to/dir
pwd

# File operations
cp source dest
mv source dest
rm file
mkdir -p dir
```