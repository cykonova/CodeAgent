using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CodeAgent.Gateway.Middleware;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;
    private readonly IConfiguration _configuration;
    
    public AuthMiddleware(
        RequestDelegate next,
        ILogger<AuthMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/ws"))
        {
            await _next(context);
            return;
        }
        
        var token = ExtractToken(context.Request);
        
        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Authorization required");
            return;
        }
        
        try
        {
            var principal = ValidateToken(token);
            context.User = principal;
            await _next(context);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid token");
        }
    }
    
    private string? ExtractToken(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring(7);
        }
        
        if (request.Query.TryGetValue("token", out var queryToken))
        {
            return queryToken.FirstOrDefault();
        }
        
        if (request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return apiKey.FirstOrDefault();
        }
        
        return null;
    }
    
    private ClaimsPrincipal ValidateToken(string token)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "default-secret-key-change-in-production";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        
        var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
        return principal;
    }
}

public static class AuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthMiddleware>();
    }
}