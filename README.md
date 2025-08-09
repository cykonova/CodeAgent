# Code Agent
The main purpose of this project is to experiment with coding agents.

## Discovery
* Maintaining the context window
  * It's more important to keep the project & technical segments of the window small and direct
  * Clearing the context/chat before each phase keeps the LLM focused
    * Remind the LLM to check PWD before starting work
* How to split up requirements
  * Requirements need to be split up into small < 1k token windows
  * An overall status and general definition file
* Useful MCP servers
  * Serena is a useful MCP.  It provides the LLM with the ability to discover relevant content within the current implementation phase.
* Review code generation as if the LLM was a team member
  * Have the LLM generate a commit for each phase
  * Review the staged files
  * Check for build errors
  * Review file placement
  * Review implementation for standards and best practice compliance
  * Adjust static context (claude.md) as needed


## Context Window
The LLM is only able to accept a specific amount of input.  This limitation
introduces a "window" effect.  This "window" is all the LLM knows, 
outside it's model training.  When the context begins to exceed 
the window content is removed.  It should be strictly managed to 
maintain performance. 

When the agent begins to go "off the rails", the context window is 
bad.  Remember you're not working with another human, no matter how
good it's responses are.  You're working with a fancy calculator.

**Context Window Content**
* Preferred Implementation Strategy
* Preferred Implementation Stack
* Coding and/or Style Guide
  * Recommend 1 file per type or thing.  This should help reduce context pollution.
  * Formatting (alignment)
    * This may be better handed off to an MCP tool.
    * While I personally love spaces over tabs, tabs use less of the context window.
    * It may be ideal to have an MCP strip whitespace, assuming the lang doesn't use whitespace at syntax
* General Project Documentation
* Implementation Status
* Current Phase Requirements
* CLI/Web tool implementing the agent coding workflow may also be injecting context
  * If the LLM wasn't trained for agent coding use, the context window will need to include directions to act as a coding agent.
  * If the tool is implementing agent roles (planner, coder, tester, etc), role directions are added to the context window.

**Context Pollution**
This can quickly cause the LLM to go way off coarse or otherwise
misbehave.  Below are some areas to watch out for
* Old documentation
* Docs with conflicting requirements
* Chat history
* local and user claude.md files
  * If the user claude.md file contains information about another project...
  * If the local (to the proj or repo) contains outdated information
* MCP Injection
  * All MCP (Model Context Provider) servers add to the context window
  * For an LLM to use an MCP, the MCP needs to add a list of tools available
  * The MCP could be malicious by design so watch out
  * The MCP could introduce malicious context injection
    * A public ticketing system could cause an MCP to inject malicious context
    * A bad actor asking for exploit implementation within the app
      * Yeah, I know there's a cybersec term for this lol

## Prompts & Workflow

When I started this rewrite, cause the init went horribly wrong, I opened
claude desktop and began explaining what I wanted to build.  Then
I quickly stopped the generation and instructed claude to only PLAN, NO CODING.
I then worked through describing the general feature list and had it make adjustments.
This approach generated large markdown docs that I downloaded an added to an empty directory. 

I then launched claude-code, had it read the files and 
break them up into smaller < 1k token phases.  I continued to work
with claude-code to refine the documentation.  Having it scan through the
docs to locate conflicts and vague requirements.  I eventually ended up
with the following project structure.
```text
.
├── docs/
│   ├── supporting/
│   │   ├── arch-agents.md
│   │   ├── arch-api.md
│   │   ├── arch-cli.md
│   │   ├── arch-deployment.md
│   │   ├── arch-frontend.md
│   │   ├── arch-gateway.md
│   │   ├── arch-ide.md
│   │   ├── arch-mcp.md
│   │   ├── arch-messaging.md
│   │   ├── arch-operations.md
│   │   ├── arch-plugins.md
│   │   ├── arch-projects.md
│   │   ├── arch-providers.md
│   │   ├── arch-sandbox.md
│   │   └── conflicts-resolved.md
│   ├── 00-project-status.md
│   ├── 01-core-infrastructure.md
│   ├── 02-provider-management.md
│   ├── 03-agent-system.md
│   ├── 04-docker-sandbox.md
│   ├── 05-project-management.md
│   ├── 06-web-portal.md
│   ├── 07-cli-tool.md
│   ├── 08-ide-extensions.md
│   ├── 09-plugin-system.md
│   └── 10-testing-deployment.md
└── src/
```

Then I cleared the chat window.
```text
/clear
```

Start the implementation of a phase
```text
Identify your current working directory. Then review the project documentation for Phase 1.  Begin implementing phase 1.
docs/00-project-status.md
docs/01-core-infrastructure.md
```

Because of my user context, claude always generates a commit 
and prompts me to approve the commit message.  I treat this prompt
like a PR code review.  I check the build, review the code and recommend
changes prior to actually committing.

## Claude Limits
After starting the rewrite for this project, on a Saturday morning.  I get
the ever so annoying `Approaching Opus usage limit · /model to use best available model`
warning at around 2p (3hrs in).  We're on Phase 5, half way through the project.
