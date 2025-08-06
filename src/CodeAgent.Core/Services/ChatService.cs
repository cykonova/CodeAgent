using System.Runtime.CompilerServices;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using System.Text;

namespace CodeAgent.Core.Services;

public class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;
    private readonly IInternalToolService _toolService;
    private readonly List<ChatMessage> _history;

    public ChatService(ILLMProvider llmProvider, IInternalToolService toolService)
    {
        _llmProvider = llmProvider;
        _toolService = toolService;
        
        // Get available tools and build tool descriptions
        var tools = _toolService.GetAvailableTools();
        var toolDescriptions = new StringBuilder();
        toolDescriptions.AppendLine("\n\nYou have access to the following tools for file and directory operations:");
        foreach (var tool in tools)
        {
            toolDescriptions.AppendLine($"- {tool.Name}: {tool.Description}");
        }
        toolDescriptions.AppendLine("\nWhen you need to perform file operations, use these tools by specifying the tool name and required parameters.");
        
        _history = new List<ChatMessage>
        {
            new ChatMessage("system", @"You are CodeAgent, a specialized coding assistant that helps developers modify and create files in their projects. 

IMPORTANT: You are NOT a chat assistant. You are a file manipulation tool. Your responses should use the provided tools to manipulate files and directories.

You have access to tools for file operations. When asked to perform any file-related task:
1. Use the appropriate tool (read_file, write_file, list_files, etc.)
2. Provide the necessary parameters for the tool
3. The tool will handle the actual file system operation

When asked to modify a file:
1. First use read_file to get the current content (if it exists)
2. Apply the requested changes
3. Use write_file to save the modified content

When asked to create a new file:
1. Use write_file with the complete file content

DO NOT return file content as text responses. Always use the appropriate tools." + toolDescriptions.ToString())
        };
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
            ToolChoice = "auto" // Let the model decide when to use tools
        };

        var response = await _llmProvider.SendMessageAsync(request, cancellationToken);
        
        // Handle tool calls if present
        if (response.ToolCalls != null && response.ToolCalls.Count > 0)
        {
            var toolResults = new StringBuilder();
            
            foreach (var toolCall in response.ToolCalls)
            {
                var result = await _toolService.ExecuteToolAsync(toolCall, cancellationToken);
                
                if (result.Success)
                {
                    toolResults.AppendLine($"Tool '{toolCall.Name}' executed successfully:");
                    toolResults.AppendLine(result.Content);
                }
                else
                {
                    toolResults.AppendLine($"Tool '{toolCall.Name}' failed: {result.Error}");
                }
                
                // Add tool response to history for context
                _history.Add(new ChatMessage("tool", result.Content, toolCall.Id));
            }
            
            // Return the tool execution results
            response.Content = toolResults.ToString();
            
            // Add assistant's tool calls to history
            _history.Add(new ChatMessage("assistant", response.Content));
        }
        else if (response.IsComplete && string.IsNullOrEmpty(response.Error))
        {
            _history.Add(new ChatMessage("assistant", response.Content));
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
            Stream = true
        };

        var fullResponse = string.Empty;
        
        await foreach (var chunk in _llmProvider.StreamMessageAsync(request, cancellationToken))
        {
            fullResponse += chunk;
            yield return chunk;
        }

        if (!string.IsNullOrEmpty(fullResponse))
        {
            _history.Add(new ChatMessage("assistant", fullResponse));
        }
    }

    public void ClearHistory()
    {
        _history.Clear();
    }

    public List<ChatMessage> GetHistory()
    {
        return new List<ChatMessage>(_history);
    }
}