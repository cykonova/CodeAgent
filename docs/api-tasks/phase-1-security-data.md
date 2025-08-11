# Phase 1: Security & Data Foundation
**Duration**: Week 1-2  
**Priority**: CRITICAL  
**Dependencies**: None

## Overview
Establish fundamental security and data persistence layers required for any production system. This phase addresses the most critical vulnerabilities that would prevent deployment.

## Tasks

### 1.1 Implement ASP.NET Identity (3 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Replace in-memory Dictionary user store with ASP.NET Identity
- Configure Identity with Entity Framework Core
- Implement proper password hashing (BCrypt/Argon2)
- Add account lockout policies
- Implement email confirmation flow

#### Implementation Steps
```csharp
// In Program.cs
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

#### Acceptance Criteria
- [ ] User registration with email verification
- [ ] Secure password storage
- [ ] Account lockout after failed attempts
- [ ] Password reset functionality
- [ ] Audit log for authentication events

### 1.2 Add Entity Framework Core (2 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Install EF Core packages for SQL Server and PostgreSQL
- Create ApplicationDbContext
- Define entity models for all domain objects
- Configure relationships and constraints
- Add migration support

#### Database Schema
```sql
-- Core tables needed
Users (from Identity)
Sessions
Providers
Agents
Projects
Workflows
AuditLogs
```

#### Implementation Steps
1. Install packages:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   dotnet add package Microsoft.EntityFrameworkCore.PostgreSQL
   dotnet add package Microsoft.EntityFrameworkCore.Tools
   ```

2. Create DbContext with proper configuration
3. Add connection string configuration
4. Create initial migration
5. Add database health checks

#### Acceptance Criteria
- [ ] DbContext properly configured
- [ ] All entities mapped
- [ ] Migrations created and tested
- [ ] Database can be created/updated via migrations
- [ ] Connection resilience configured

### 1.3 Externalize Secrets (1 day)
**Owner**: DevOps Team  
**Complexity**: Low

#### Requirements
- Remove all hardcoded secrets from code
- Implement Azure Key Vault or AWS Secrets Manager integration
- Support local development with User Secrets
- Document secret rotation process

#### Configuration Structure
```json
{
  "Authentication": {
    "Jwt": {
      "SecretKey": "{{from-key-vault}}",
      "Issuer": "CodeAgent",
      "Audience": "CodeAgentAPI",
      "ExpirationMinutes": 60
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "{{from-key-vault}}"
  },
  "Providers": {
    "Anthropic": {
      "ApiKey": "{{from-key-vault}}"
    },
    "OpenAI": {
      "ApiKey": "{{from-key-vault}}"
    }
  }
}
```

#### Implementation Steps
1. Configure Key Vault provider
2. Set up Managed Identity (Azure) or IAM roles (AWS)
3. Implement IConfiguration with secret provider
4. Add secret rotation notification handling
5. Update deployment scripts

#### Acceptance Criteria
- [ ] No secrets in source code
- [ ] Key Vault integration working
- [ ] Local development uses User Secrets
- [ ] Secret rotation documented
- [ ] Deployment scripts updated

### 1.4 Implement Input Validation (2 days)
**Owner**: Backend Team  
**Complexity**: Medium

#### Requirements
- Add FluentValidation to all projects
- Create validators for all DTOs/requests
- Implement validation middleware
- Add custom validation rules for business logic
- Configure model state validation

#### Example Validator
```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email already registered");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase")
            .Matches(@"[a-z]").WithMessage("Must contain lowercase")
            .Matches(@"[0-9]").WithMessage("Must contain number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain special character");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[a-zA-Z\s]+$");
    }
}
```

#### Acceptance Criteria
- [ ] All API endpoints validate input
- [ ] Custom business rule validation
- [ ] Consistent error response format
- [ ] Validation errors logged
- [ ] Client-friendly error messages

### 1.5 Configure CORS Properly (1 day)
**Owner**: Backend Team  
**Complexity**: Low

#### Requirements
- Remove AllowAnyOrigin
- Configure specific allowed origins
- Environment-specific CORS policies
- Support preflight requests properly
- Add CORS configuration to appsettings

#### Implementation
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "https://codeagent.com",
                "https://app.codeagent.com"
            )
            .AllowCredentials()
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
            .SetPreflightMaxAge(TimeSpan.FromHours(24));
    });

    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:3000"
            )
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
```

#### Acceptance Criteria
- [ ] CORS configured per environment
- [ ] Specific origins whitelisted
- [ ] Preflight requests handled
- [ ] WebSocket CORS configured
- [ ] Documentation updated

## Testing Requirements

### Unit Tests
- Identity service tests
- Validation logic tests
- DbContext configuration tests
- Secret provider tests

### Integration Tests
- Authentication flow end-to-end
- Database operations
- CORS policy verification
- Validation middleware tests

## Security Checklist
- [ ] OWASP Top 10 vulnerabilities addressed
- [ ] SQL injection prevention verified
- [ ] XSS protection enabled
- [ ] CSRF tokens implemented
- [ ] Security headers configured
- [ ] TLS/SSL properly configured
- [ ] Sensitive data encrypted at rest
- [ ] PII handling compliant with GDPR

## Migration Plan
1. **Data Migration**
   - Export existing in-memory data
   - Create migration scripts
   - Test migration in staging
   - Plan zero-downtime migration

2. **Rollback Strategy**
   - Database backup before migration
   - Feature flags for gradual rollout
   - Ability to revert to in-memory mode
   - Migration rollback scripts ready

## Success Metrics
- Zero hardcoded secrets in codebase
- All API calls validated
- Database response time < 100ms for 95% of queries
- Zero authentication bypass vulnerabilities
- 100% of endpoints have input validation

## Risk Mitigation
| Risk | Mitigation |
|------|------------|
| Data loss during migration | Comprehensive backup strategy |
| Performance degradation | Database indexing and query optimization |
| Breaking changes for clients | API versioning from start |
| Secret exposure | Audit logs and rotation policies |

## Dependencies
- Database server provisioned
- Key Vault/Secrets Manager available
- Identity provider configured (if using external)
- Email service for notifications

## Deliverables
- [ ] Updated Program.cs with security configuration
- [ ] Database schema and migrations
- [ ] Validation rules for all endpoints
- [ ] Security configuration documentation
- [ ] Migration scripts and procedures
- [ ] Updated appsettings.json structure
- [ ] Security audit report

## Notes
- Consider using IdentityServer4/Duende for more complex scenarios
- Evaluate need for MFA in this phase or Phase 2
- Document all security decisions in ADRs
- Schedule security review after implementation