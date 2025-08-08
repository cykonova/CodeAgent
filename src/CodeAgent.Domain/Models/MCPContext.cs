using System;
using System.Collections.Generic;

namespace CodeAgent.Domain.Models
{
    public class MCPContext
    {
        public string SessionId { get; set; } = string.Empty;
        public List<MCPMessage> Messages { get; set; } = new List<MCPMessage>();
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        public List<MCPResource> Resources { get; set; } = new List<MCPResource>();
        public MCPSettings? Settings { get; set; }
    }
    
    public class MCPMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
    
    public class MCPResource
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Uri { get; set; }
        public string? Content { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
    
    public class MCPSettings
    {
        public int MaxContextLength { get; set; }
        public bool EnableTools { get; set; }
        public bool EnableResources { get; set; }
        public List<string>? AllowedTools { get; set; }
    }

}