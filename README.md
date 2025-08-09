# Code Agent
The main purpose of this project is to experiment with coding agents.

## Discovery
* Maintaining the context window
  * It's more important to keep the project & technical segments of the window small and direct
  * Clearing the context/chat before each phase keeps the LLM focused
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
* Formatting (alignment)
* General Project Documentation
* Implementation Status
* Current Phase Requirements
