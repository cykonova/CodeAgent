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
This "window" is all the LLM knows, outside it's model training.  This window should 
be strictly managed to maintain performance.

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

**Context Pollution**
This can quickly cause the LLM to go way off coarse or otherwise
misbehave.  Below are some areas to watch out for
* Old documentation
* Docs with conflicting requirements
* Chat history
* local and user claude.md files
  * If the user claude.md file contains information about another project...
  * If the local (to the proj or repo) contains outdated information
* MCP & CLI Injection
  * All MCP (Model Context Provider) servers add to the context window
  * For an LLM to use an MCP, the MCP needs to add a list of tools available
  * The MCP could be malicious by design so watch out
  * The MCP could introduce malicious context injection
    * A public ticketing system could cause an MCP to inject malicious context
    * A bad actor asking for exploit implementation within the app
      * Yeah, I know there's a cybersec term for this lol
  * CLI/Web tool implementing the agent coding workflow may also be injecting context
    * If the LLM wasn't trained for agent coding use, the context window will need to include directions to act as a coding agent.
    * If the tool is implementing agent roles (planner, coder, tester, etc), role directions are added to the context window.
