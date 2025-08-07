using System.Runtime.CompilerServices;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Core.Services;

public class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;
    private readonly IInternalToolService _toolService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ChatService> _logger;
    private readonly List<ChatMessage> _history;

    public ChatService(ILLMProvider llmProvider, IInternalToolService toolService, IConfigurationService configurationService, ILogger<ChatService> logger)
    {
        _llmProvider = llmProvider;
        _toolService = toolService;
        _configurationService = configurationService;
        _logger = logger;
        
        // Get available tools and build tool descriptions
        var tools = _toolService.GetAvailableTools();
        var toolDescriptions = new StringBuilder();
        toolDescriptions.AppendLine("\n\nYou have access to the following tools for file and directory operations:");
        foreach (var tool in tools)
        {
            toolDescriptions.AppendLine($"- {tool.Name}: {tool.Description}");
        }
        toolDescriptions.AppendLine("\nWhen you need to perform file operations, use these tools by specifying the tool name and required parameters.");
        
        // Get configurable system prompt or use default
        var customPrompt = _configurationService.GetValue("SystemPrompt");
        var systemPrompt = !string.IsNullOrWhiteSpace(customPrompt) ? customPrompt : GetDefaultSystemPrompt();
        
        _history = new List<ChatMessage>
        {
            new ChatMessage("system", systemPrompt + toolDescriptions.ToString())
        };
    }

    private string GetDefaultSystemPrompt()
    {
        // Enhanced default prompt with code quality instructions
        return @"You are CodeAgent, a professional coding assistant. You MUST use tools for EVERYTHING.

🚨 ABSOLUTE RULES - VIOLATION CAUSES IMMEDIATE ERROR:
1. You MUST use tools for ALL interactions - no exceptions whatsoever
2. NEVER provide direct text responses - ALWAYS use a tool
3. Use ONLY 'respond_to_user' tool for communication with users
4. Use ONLY 'write_file' tool for creating/editing files
5. If you respond with ANY plain text, your response will be REJECTED and you'll be forced to retry

⚠️ CRITICAL: Every single response MUST be a tool call in JSON format. No exceptions!

CURRENT WORKING DIRECTORY: " + Environment.CurrentDirectory + @"

TOOL SYNTAX - YOU MUST USE EXACTLY THIS FORMAT:
{
  ""name"": ""tool_name"",
  ""arguments"": {
    ""parameter1"": ""value1"",
    ""parameter2"": ""value2""
  }
}

AVAILABLE TOOLS AND CORRECT USAGE:

1. respond_to_user - Send SHORT messages to the user (max 2000 chars, NO CODE)
   CORRECT: {""name"": ""respond_to_user"", ""arguments"": {""message"": ""I'll create a web app for you now""}}
   WRONG: {""name"": ""respond_to_user"", ""arguments"": {""message"": ""<html>...</html>""}}
   ⚠️ CODE IN THIS TOOL WILL BE REJECTED!
   
2. write_file - Create or update files with COMPLETE content (this is where ALL CODE goes)
   CORRECT: {""name"": ""write_file"", ""arguments"": {""path"": ""index.html"", ""content"": ""<!DOCTYPE html>\n<html>...</html>""}}
   WRONG: Showing code snippets in respond_to_user
   ✅ ALL CODE MUST GO HERE, NOT IN respond_to_user!

3. create_directory - Create a new directory
   CORRECT: {""name"": ""create_directory"", ""arguments"": {""path"": ""folder_name""}}
   
4. list_files - List files in a directory
   CORRECT: {""name"": ""list_files"", ""arguments"": {""path"": "".""}}
   
5. read_file - Read a file's contents
   CORRECT: {""name"": ""read_file"", ""arguments"": {""path"": ""file.txt""}}

6. execute_bash - Execute bash commands (npm, git, dotnet, etc.)
   CORRECT: {""name"": ""execute_bash"", ""arguments"": {""command"": ""npm install""}}
   CORRECT: {""name"": ""execute_bash"", ""arguments"": {""command"": ""npm run build"", ""working_directory"": ""./frontend""}}
   Note: Only safe commands are allowed. Dangerous operations will be blocked

WHEN BUILDING APPLICATIONS - FOLLOW THIS PATTERN:
1. Use respond_to_user to briefly acknowledge the request (e.g., ""Creating your web app..."")
2. Use create_directory if subdirectories are needed
3. Use write_file with COMPLETE file content for EACH file (ALL CODE GOES HERE)
4. NEVER show code in respond_to_user messages
5. Use respond_to_user at the end to briefly summarize (e.g., ""Created 3 files: index.html, style.css, script.js"")

EXAMPLE OF CORRECT BEHAVIOR:
User: Build a web app
Your response:
{""name"": ""respond_to_user"", ""arguments"": {""message"": ""Creating a web application for you...""}}
{""name"": ""write_file"", ""arguments"": {""path"": ""index.html"", ""content"": ""<!DOCTYPE html>\n<html lang=\""en\"">\n<head>\n    <meta charset=\""UTF-8\"">\n    <title>My App</title>\n    <link rel=\""stylesheet\"" href=\""style.css\"">\n</head>\n<body>\n    <h1>Welcome to My App</h1>\n    <script src=\""script.js\""></script>\n</body>\n</html>""}}
{""name"": ""write_file"", ""arguments"": {""path"": ""style.css"", ""content"": ""body {\n    font-family: Arial, sans-serif;\n    margin: 0;\n    padding: 20px;\n    background: #f0f0f0;\n}\n\nh1 {\n    color: #333;\n}""}}
{""name"": ""write_file"", ""arguments"": {""path"": ""script.js"", ""content"": ""// Application logic\nfunction init() {\n    console.log('App initialized');\n    // Add your JavaScript code here\n}\n\n// Initialize when DOM is ready\nif (document.readyState === 'loading') {\n    document.addEventListener('DOMContentLoaded', init);\n} else {\n    init();\n}""}}
{""name"": ""respond_to_user"", ""arguments"": {""message"": ""✅ Created web application with 3 files: index.html, style.css, and script.js""}}

REMEMBER:
- respond_to_user is for SHORT MESSAGES ONLY (max 2000 chars)
- write_file is where ALL CODE must go
- NEVER mix code with communication - they use different tools
- If you put code in respond_to_user, it WILL BE REJECTED";
    }

    public async Task<ChatResponse> ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        var userMessage = new ChatMessage("user", message);
        _history.Add(userMessage);

        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>(_history),
            Stream = false,
            Tools = _toolService.GetAvailableTools(),
            ToolChoice = "required" // FORCE the model to use tools
        };

        ChatResponse response;
        int maxIterations = 10; // Prevent infinite loops
        int iteration = 0;
        
        do
        {
            response = await _llmProvider.SendMessageAsync(request, cancellationToken);
            iteration++;
            
            // Validate that the response contains tool calls
            if (response.ToolCalls == null || response.ToolCalls.Count == 0)
            {
                // LLM didn't use tools - this is an error
                if (!string.IsNullOrEmpty(response.Content))
                {
                    // Alert the user about the invalid response
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("⚠️  LLM sent invalid response - forcing tool usage...");
                    Console.ResetColor();
                    
                    // Check if response looks like malformed tool syntax or raw JSON tool calls
                    var responseText = response.Content?.ToLower() ?? "";
                    if (responseText.Contains("write_file(") || responseText.Contains("respond_to_user(") || 
                        responseText.Contains("create_directory(") || responseText.Contains("import ") ||
                        responseText.Contains("os.makedirs") || 
                        (responseText.Contains("{\"name\":") && responseText.Contains("\"arguments\":")) ||
                        (responseText.Contains("\"parameters\":") && responseText.Contains("\"name\":")))
                    {
                        // Build detailed error with tool list
                        var toolList = new StringBuilder();
                        toolList.AppendLine("ERROR: Invalid tool syntax detected! You MUST use JSON format for tools.");
                        toolList.AppendLine();
                        toolList.AppendLine("AVAILABLE TOOLS (use these EXACTLY):");
                        
                        foreach (var tool in _toolService.GetAvailableTools())
                        {
                            toolList.AppendLine($"- {tool.Name}: {tool.Description}");
                            if (tool.Parameters != null && tool.Parameters.Any())
                            {
                                toolList.Append("  Parameters: ");
                                toolList.AppendLine(string.Join(", ", tool.Parameters.Keys));
                            }
                        }
                        
                        toolList.AppendLine();
                        toolList.AppendLine("CORRECT FORMAT:");
                        toolList.AppendLine("{\"name\": \"respond_to_user\", \"arguments\": {\"message\": \"Your message here\"}}");
                        toolList.AppendLine("{\"name\": \"write_file\", \"arguments\": {\"path\": \"filename.ext\", \"content\": \"Full file content here\"}}");
                        toolList.AppendLine();
                        toolList.AppendLine("NEVER use function() syntax, import statements, or show code outside of write_file!");
                        
                        _history.Add(new ChatMessage("system", toolList.ToString()));
                    }
                    else
                    {
                        // Generic error for non-tool responses
                        _history.Add(new ChatMessage("system", 
                            "ERROR: You MUST use tools. Use 'respond_to_user' to send messages. " +
                            "Use 'write_file' to create files. NEVER respond with plain text."));
                    }
                    
                    // Resend the user's original message with error guidance
                    // Keep only system prompt and original user message, plus error
                    var systemMessage = _history.FirstOrDefault(m => m.Role == "system");
                    var userMsg = _history.FirstOrDefault(m => m.Role == "user");
                    
                    var retryMessages = new List<ChatMessage>();
                    if (systemMessage != null) retryMessages.Add(systemMessage);
                    if (userMsg != null) retryMessages.Add(userMsg);
                    retryMessages.Add(_history.Last()); // Add the error message we just created
                    
                    request.Messages = retryMessages;
                    continue; // Retry
                }
            }
            
            // Handle tool calls if present
            if (response.ToolCalls != null && response.ToolCalls.Count > 0)
            {
                // Execute each tool call
                foreach (var toolCall in response.ToolCalls)
                {
                    var result = await _toolService.ExecuteToolAsync(toolCall, cancellationToken);
                    
                    // Add tool response to history for context
                    var toolMessage = result.Success ? result.Content : $"Error: {result.Error}";
                    
                    // If the tool failed due to code in respond_to_user, add explicit guidance
                    if (!result.Success && result.Error != null && result.Error.Contains("appears you're trying to send code"))
                    {
                        toolMessage += "\n\nREMINDER: You MUST use the 'write_file' tool for ALL code content. " +
                                      "The 'respond_to_user' tool is ONLY for short messages to communicate with the user. " +
                                      "Please retry: use write_file for code, respond_to_user for brief messages only.";
                    }
                    
                    _history.Add(new ChatMessage("tool", toolMessage, toolCall.Id));
                }
                
                // Update request with new history for next iteration
                request.Messages = new List<ChatMessage>(_history);
            }
            else
            {
                // No more tool calls, we're done
                break;
            }
        }
        while (iteration < maxIterations && response.ToolCalls?.Count > 0);
        
        // Check if the last tool response was a user message
        var lastToolResponse = _history.LastOrDefault(m => m.Role == "tool");
        if (lastToolResponse != null)
        {
            // Extract user messages from respond_to_user tool responses
            var userMessages = new List<string>();
            for (int i = _history.Count - 1; i >= 0 && _history[i].Role == "tool"; i--)
            {
                var toolContent = _history[i].Content;
                var extractedMessage = ExtractMessageFromToolResponse(toolContent);
                if (!string.IsNullOrEmpty(extractedMessage))
                {
                    userMessages.Insert(0, extractedMessage);
                }
            }
            
            if (userMessages.Any())
            {
                // Combine all user messages and return as the response
                var combinedMessage = string.Join("\n", userMessages);
                return new ChatResponse
                {
                    Content = combinedMessage,
                    IsComplete = true
                };
            }
        }
        
        if (response.IsComplete && string.IsNullOrEmpty(response.Error) && !string.IsNullOrEmpty(response.Content))
        {
            // Check if the LLM provided direct content instead of using tools - this violates the tool-only rule
            _logger.LogWarning("LLM provided direct content instead of using tools: {Content}", response.Content);
            
            // Check if the content looks like raw JSON tool calls that shouldn't be shown to user
            var contentLower = response.Content.ToLower();
            if ((contentLower.Contains("{\"name\":") && contentLower.Contains("\"arguments\":")) ||
                (contentLower.Contains("\"parameters\":") && contentLower.Contains("\"name\":")))
            {
                // Don't return raw JSON to the user
                return new ChatResponse
                {
                    Content = "I encountered an issue with the response format. Please try rephrasing your request.",
                    IsComplete = true,
                    Error = "Invalid response format from LLM"
                };
            }
            
            // If LLM provided direct content, this violates the tool-only constraint
            // Force it to use tools by providing an error and retrying
            _history.Add(new ChatMessage("system", 
                "ERROR: You provided a direct response instead of using tools. You MUST use tools for ALL interactions. " +
                "Use 'respond_to_user' tool to communicate with the user. NEVER respond with plain text."));
            
            // Retry with the original user message
            var systemMessage = _history.FirstOrDefault(m => m.Role == "system");
            var userMsg = _history.FirstOrDefault(m => m.Role == "user");
            
            var retryMessages = new List<ChatMessage>();
            if (systemMessage != null) retryMessages.Add(systemMessage);
            if (userMsg != null) retryMessages.Add(userMsg);
            retryMessages.Add(_history.Last()); // Add the error message we just created
            
            // Retry once more with tool enforcement
            var retryRequest = new ChatRequest
            {
                Messages = retryMessages,
                Stream = false,
                Tools = _toolService.GetAvailableTools(),
                ToolChoice = "required" // FORCE tool usage
            };
            
            var retryResponse = await _llmProvider.SendMessageAsync(retryRequest, cancellationToken);
            if (retryResponse.ToolCalls != null && retryResponse.ToolCalls.Count > 0)
            {
                // Process the retry response with tools
                foreach (var toolCall in retryResponse.ToolCalls)
                {
                    var result = await _toolService.ExecuteToolAsync(toolCall, cancellationToken);
                    var toolMessage = result.Success ? result.Content : $"Error: {result.Error}";
                    _history.Add(new ChatMessage("tool", toolMessage, toolCall.Id));
                }
                
                // Extract user messages from the tool responses
                var userMessages = new List<string>();
                for (int i = _history.Count - 1; i >= 0 && _history[i].Role == "tool"; i--)
                {
                    var toolContent = _history[i].Content;
                    var extractedMessage = ExtractMessageFromToolResponse(toolContent);
                    if (!string.IsNullOrEmpty(extractedMessage))
                    {
                        userMessages.Insert(0, extractedMessage);
                    }
                }
                
                if (userMessages.Any())
                {
                    return new ChatResponse
                    {
                        Content = string.Join("\n", userMessages),
                        IsComplete = true
                    };
                }
            }
            
            // If retry also failed, return a fallback message
            return new ChatResponse
            {
                Content = "I need to use tools to respond properly. Please try your request again.",
                IsComplete = true,
                Error = "Failed to enforce tool usage"
            };
        }

        return response;
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userMessage = new ChatMessage("user", message);
        _history.Add(userMessage);

        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>(_history),
            Stream = true,
            Tools = _toolService.GetAvailableTools(),
            ToolChoice = "auto"
        };

        var content = new StringBuilder();
        
        await foreach (var chunk in _llmProvider.StreamMessageAsync(request, cancellationToken))
        {
            content.Append(chunk);
            yield return chunk;
        }

        _history.Add(new ChatMessage("assistant", content.ToString()));
    }

    public void ClearHistory()
    {
        var systemMessage = _history.FirstOrDefault(m => m.Role == "system");
        _history.Clear();
        if (systemMessage != null)
        {
            _history.Add(systemMessage);
        }
    }

    public List<ChatMessage> GetHistory()
    {
        return new List<ChatMessage>(_history);
    }

    private string ExtractMessageFromToolResponse(string toolContent)
    {
        if (string.IsNullOrWhiteSpace(toolContent))
            return string.Empty;

        // Check if this is a JSON tool call response
        try
        {
            if (toolContent.TrimStart().StartsWith("{"))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(toolContent);
                if (doc.RootElement.TryGetProperty("name", out var nameElement))
                {
                    var toolName = nameElement.GetString();
                    if (doc.RootElement.TryGetProperty("arguments", out var argsElement))
                    {
                        switch (toolName)
                        {
                            case "respond_to_user":
                                if (argsElement.TryGetProperty("message", out var messageElement))
                                {
                                    return messageElement.GetString() ?? string.Empty;
                                }
                                break;

                            case "write_file":
                                if (argsElement.TryGetProperty("path", out var pathElement))
                                {
                                    var filePath = pathElement.GetString() ?? "unknown file";
                                    return $"📁 Created file: `{filePath}`";
                                }
                                break;

                            case "read_file":
                                if (argsElement.TryGetProperty("path", out var readPathElement))
                                {
                                    var filePath = readPathElement.GetString() ?? "unknown file";
                                    return $"📖 Read file: `{filePath}`";
                                }
                                break;

                            case "execute_bash":
                                if (argsElement.TryGetProperty("command", out var commandElement))
                                {
                                    var command = commandElement.GetString() ?? "unknown command";
                                    return $"🔨 Executed: `{command}`";
                                }
                                break;

                            case "create_directory":
                                if (argsElement.TryGetProperty("path", out var dirPathElement))
                                {
                                    var dirPath = dirPathElement.GetString() ?? "unknown directory";
                                    return $"📂 Created directory: `{dirPath}`";
                                }
                                break;

                            case "list_files":
                                if (argsElement.TryGetProperty("path", out var listPathElement))
                                {
                                    var listPath = listPathElement.GetString() ?? ".";
                                    return $"📋 Listed files in: `{listPath}`";
                                }
                                break;

                            default:
                                return $"🔧 Used tool: `{toolName}`";
                        }
                    }
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not valid JSON, treat as plain text
        }

        // If it's not a tool call JSON, this might be tool execution results
        // Return the content as-is (this could be file contents, command output, etc.)
        return toolContent;
    }
}