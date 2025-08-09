# Documentation Conflicts to Resolve

## Critical Conflicts

### 1. Frontend Technology Stack
**Conflict**: Mixed references to Angular, React, and Blazor
- **Location 1**: `00-project-status.md` line 65 mentions "React/Blazor UI setup"  
- **Location 2**: `06-web-portal.md` specifies Angular with Nx.dev throughout
- **Resolution Needed**: Confirm Angular as the chosen framework and update status doc

### 2. Message Architecture Pattern
**Conflict**: Gateway flow vs pub/sub messaging
- **Location 1**: `arch-gateway.md` shows linear message flow
- **Location 2**: `arch-messaging.md` shows topic-based pub/sub
- **Resolution Needed**: Clarify if using direct routing or event-driven architecture

### 3. Database Strategy
**Conflict**: SQLite vs PostgreSQL as default
- **Location 1**: `arch-operations.md` states SQLite as default
- **Location 2**: Deployment docs only reference SQLite
- **Resolution Needed**: Define clear database strategy and migration path

## Moderate Conflicts

### 4. Agent Temperature Settings
**Conflict**: Different temperature values for same agent types
- **Location 1**: `03-agent-system.md` lines 92-99
- **Location 2**: `arch-agents.md` lines 32-38
- **Resolution Needed**: Standardize temperature settings per agent type

### 5. Plugin Integration Approach
**Conflict**: Unified API vs interface-specific adapters
- **Location 1**: `09-plugin-system.md` describes unified API
- **Location 2**: `arch-plugins.md` shows separate adapters
- **Resolution Needed**: Choose consistent plugin architecture

### 6. Cost Management Defaults
**Conflict**: Inconsistent default limits
- **Location 1**: `arch-cost-management.md` - $500 global monthly
- **Location 2**: `arch-api.md` - Different tier limits
- **Resolution Needed**: Align rate limits with cost limits

## Minor Conflicts

### 7. Backend Implementation Language
**Conflict**: C# vs TypeScript examples
- **Location 1**: `01-core-infrastructure.md` specifies .NET 8/C#
- **Location 2**: Some examples show TypeScript
- **Resolution Needed**: Confirm C# for backend, TypeScript for frontend only

### 8. Container Resource Limits
**Conflict**: Missing vs specified resource limits
- **Location 1**: `04-docker-sandbox.md` - no limits specified
- **Location 2**: `arch-sandbox.md` - specific CPU/memory limits
- **Resolution Needed**: Add resource limits to main sandbox doc

### 9. Provider Pricing Data
**Conflict**: Pricing info scattered and potentially outdated
- **Location 1**: `arch-cost-management.md` - detailed pricing
- **Location 2**: `arch-providers.md` - no pricing integration
- **Resolution Needed**: Centralize and update pricing information

## Recommendations

1. **Immediate Actions**:
   - Update `00-project-status.md` to remove React/Blazor references
   - Choose between gateway routing and pub/sub messaging
   - Standardize on SQLite with PostgreSQL as scale-up option

2. **Documentation Standards**:
   - Create a single source of truth for configuration values
   - Use consistent examples across all docs
   - Add version/last-updated dates to track changes

3. **Technical Decisions**:
   - Frontend: Angular with Nx.dev (confirmed)
   - Backend: .NET 8/C# (confirmed)
   - Database: SQLite default, PostgreSQL for production
   - Messaging: Event-driven with fallback to direct routing
   - CLI: Spectre.Console (confirmed)