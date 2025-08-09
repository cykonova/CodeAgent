# Task Completion Checklist

When completing a development task, always:

## Before Committing Code
1. **Build the solution** - Run `dotnet build` to ensure no compilation errors
2. **Run tests** - Execute `dotnet test` for backend, `nx test` for frontend
3. **Check linting** - Run appropriate lint commands
4. **Verify warnings** - Ensure no warnings (they're treated as errors)

## After Implementation
1. **Update project status** - Mark tasks as completed in docs/00-project-status.md
2. **Add new files to git** - Use `git add` for any new files created
3. **Commit changes** - Create meaningful commit messages
4. **Test the feature** - Verify the implementation works as expected

## Documentation Updates
1. **Update phase documentation** if requirements change
2. **Mark completed tasks** with [x] in status document
3. **Update phase status** (Not Started → In Progress → Completed)

## Quality Checks
1. **Follow SOLID principles** in all code
2. **Ensure proper DI** for services
3. **No hardcoded values** - use configuration
4. **Proper error handling** throughout
5. **Unit test coverage** meets 80% minimum

## Important Reminders
- DO NOT alter project docs without approval
- Never add features without permission (Product Owner constraint)
- Always use serena tools for project management
- Test changes with builds and unit tests