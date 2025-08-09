using System.Text;
using CodeAgent.Gateway.Gateway;
using CodeAgent.Gateway.Middleware;
using CodeAgent.Gateway.Models;
using CodeAgent.Gateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<MessageRouter>();
builder.Services.AddSingleton<WebSocketHandler>();

// In-memory user store for development
builder.Services.AddSingleton<Dictionary<string, (string password, string firstName, string lastName, string userId)>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production-minimum-32-characters";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.UseAuthMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTimeOffset.UtcNow,
    service = "CodeAgent.Gateway"
}));

// Auth endpoints
app.MapPost("/api/auth/register", (RegisterRequest request, Dictionary<string, (string password, string firstName, string lastName, string userId)> userStore) =>
{
    // Check if email already exists
    if (userStore.ContainsKey(request.Email.ToLower()))
    {
        return Results.BadRequest(new { error = "Email already registered" });
    }
    
    // Store user (in production, hash the password)
    var userId = Guid.NewGuid().ToString();
    userStore[request.Email.ToLower()] = (request.Password, request.FirstName, request.LastName, userId);
    
    var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production-minimum-32-characters";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var claims = new[]
    {
        new System.Security.Claims.Claim("email", request.Email),
        new System.Security.Claims.Claim("name", $"{request.FirstName} {request.LastName}"),
        new System.Security.Claims.Claim("sub", userId)
    };
    
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: "CodeAgent",
        audience: "CodeAgent",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds);
    
    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    
    var refreshToken = Guid.NewGuid().ToString();
    
    return Results.Ok(new AuthResponse
    { 
        Token = tokenString,
        RefreshToken = refreshToken,
        ExpiresIn = 86400, // 24 hours in seconds
        User = new UserDto
        {
            Id = userId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Roles = new[] { "user" }
        }
    });
});

app.MapPost("/api/auth/login", (LoginRequest request, Dictionary<string, (string password, string firstName, string lastName, string userId)> userStore) =>
{
    // Check if user exists and password matches
    if (!userStore.TryGetValue(request.Email.ToLower(), out var userData) || userData.password != request.Password)
    {
        return Results.Unauthorized();
    }
    
    var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production-minimum-32-characters";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var claims = new[]
    {
        new System.Security.Claims.Claim("email", request.Email),
        new System.Security.Claims.Claim("name", $"{userData.firstName} {userData.lastName}"),
        new System.Security.Claims.Claim("sub", userData.userId)
    };
    
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: "CodeAgent",
        audience: "CodeAgent",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds);
    
    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    
    var refreshToken = Guid.NewGuid().ToString();
    
    return Results.Ok(new AuthResponse
    { 
        Token = tokenString,
        RefreshToken = refreshToken,
        ExpiresIn = 86400, // 24 hours in seconds
        User = new UserDto
        {
            Id = userData.userId,
            Email = request.Email,
            FirstName = userData.firstName,
            LastName = userData.lastName,
            Roles = new[] { "user" }
        }
    });
});

app.MapPost("/api/auth/refresh", (RefreshTokenRequest request) =>
{
    // For now, we'll generate a new token for any refresh token
    // In production, this would validate the refresh token
    var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production-minimum-32-characters";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: "CodeAgent",
        audience: "CodeAgent",
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds);
    
    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    
    return Results.Ok(new AuthResponse
    { 
        Token = tokenString,
        RefreshToken = request.RefreshToken,
        ExpiresIn = 86400
    });
});

app.MapGet("/api/auth/token", () =>
{
    var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production-minimum-32-characters";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: "CodeAgent",
        audience: "CodeAgent",
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds);
    
    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    
    return Results.Ok(new 
    { 
        token = tokenString,
        expiresAt = token.ValidTo
    });
});

// API endpoints for frontend services
app.MapGet("/api/agents", () =>
{
    return Results.Ok(new[]
    {
        new { id = "1", name = "Code Assistant", type = "assistant", status = "online", description = "General purpose coding assistant" },
        new { id = "2", name = "Test Runner", type = "tester", status = "offline", description = "Automated test execution agent" }
    });
}).RequireAuthorization();

app.MapGet("/api/projects", () =>
{
    return Results.Ok(new[]
    {
        new { id = "1", name = "Sample Project", status = "active", description = "Demo project", createdAt = DateTime.UtcNow.AddDays(-7) }
    });
}).RequireAuthorization();

app.MapGet("/api/providers", () =>
{
    return Results.Ok(new[]
    {
        new { 
            id = "anthropic", 
            name = "Anthropic", 
            enabled = true, 
            status = new { isConnected = true, message = "Connected" },
            models = new[] { "claude-3-opus", "claude-3-sonnet" }
        },
        new { 
            id = "openai", 
            name = "OpenAI", 
            enabled = false, 
            status = new { isConnected = false, message = "Not configured" },
            models = new[] { "gpt-4", "gpt-3.5-turbo" }
        }
    });
}).RequireAuthorization();

app.MapGet("/api/workflows", () =>
{
    return Results.Ok(new[]
    {
        new { id = "1", name = "Code Review", description = "Automated code review workflow" },
        new { id = "2", name = "Test Generation", description = "Generate unit tests for code" }
    });
}).RequireAuthorization();

app.Map("/ws", async (HttpContext context, WebSocketHandler handler) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleAsync(webSocket, context.RequestAborted);
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket connections only");
    }
});

app.Run();