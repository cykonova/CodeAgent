using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CodeAgent.Web.Hubs;
using System.Runtime.InteropServices;

namespace CodeAgent.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IHubContext<AgentHub> _agentHub;
        private readonly IHubContext<CollaborationHub> _collaborationHub;
        
        public HealthController(
            ILogger<HealthController> logger,
            IHubContext<AgentHub> agentHub,
            IHubContext<CollaborationHub> collaborationHub)
        {
            _logger = logger;
            _agentHub = agentHub;
            _collaborationHub = collaborationHub;
        }
        
        [HttpGet]
        public IActionResult Get()
        {
            var health = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Environment = new
                {
                    MachineName = Environment.MachineName,
                    OSDescription = RuntimeInformation.OSDescription,
                    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    IsDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true",
                    AspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    AspNetCoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                },
                SignalR = new
                {
                    AgentHubAvailable = _agentHub != null,
                    CollaborationHubAvailable = _collaborationHub != null
                },
                WebSocket = new
                {
                    WebSocketsSupported = HttpContext.WebSockets.IsWebSocketRequest || true,
                    Headers = new
                    {
                        Upgrade = Request.Headers["Upgrade"].ToString(),
                        Connection = Request.Headers["Connection"].ToString(),
                        SecWebSocketVersion = Request.Headers["Sec-WebSocket-Version"].ToString()
                    }
                }
            };
            
            _logger.LogInformation("Health check performed: {@Health}", health);
            
            return Ok(health);
        }
        
        [HttpGet("websocket-test")]
        public async Task WebSocketTest()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _logger.LogInformation("WebSocket connection test accepted");
                
                var buffer = new byte[1024 * 4];
                var receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
                
                while (!receiveResult.CloseStatus.HasValue)
                {
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                        receiveResult.MessageType,
                        receiveResult.EndOfMessage,
                        CancellationToken.None);
                    
                    receiveResult = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                
                await webSocket.CloseAsync(
                    receiveResult.CloseStatus.Value,
                    receiveResult.CloseStatusDescription,
                    CancellationToken.None);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsync("Not a WebSocket request");
            }
        }
    }
}