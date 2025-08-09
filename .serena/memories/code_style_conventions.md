# Code Style and Conventions

## C# / .NET Standards
- Target Framework: .NET 8.0
- Nullable: enabled
- ImplicitUsings: enabled
- TreatWarningsAsErrors: true
- Minimal API pattern for all endpoints
- Dependency injection for all services
- Channel-based async communication
- JWT authentication with rate limiting
- 1 Type per file
- Follow SOLID principles

## Angular/TypeScript Standards
- Angular 20.1 with Nx.dev
- TypeScript with strict mode
- Module Federation architecture
- Maximum 100 lines per component file
- Use Material components for ALL UI elements
- No hardcoded colors - use theme variables only
- Support light/dark mode switching
- All text must be externalized for i18n

## Testing Requirements
- Unit test coverage minimum: 80%
- Integration tests for all API endpoints
- WebSocket connection tests required
- Mock providers for external services
- Test Driven Development approach

## Git Workflow
- After completing each request, add newly created files to git
- Then commit with appropriate message
- No A/B testing with models
- Always use LTS frameworks if available