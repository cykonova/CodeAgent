using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using CodeAgent.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace CodeAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;
    private readonly ISecurityService _securityService;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        IAntiforgery antiforgery,
        ISecurityService securityService,
        ILogger<SecurityController> logger)
    {
        _antiforgery = antiforgery;
        _securityService = securityService;
        _logger = logger;
    }

    [HttpGet("csrf-token")]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { csrfToken = tokens.RequestToken });
    }

    [HttpPost("validate-path")]
    public async Task<IActionResult> ValidatePath([FromBody] ValidatePathRequest request)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrEmpty(request.Path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            // Sanitize path
            var sanitizedPath = SanitizePath(request.Path);
            if (sanitizedPath != request.Path)
            {
                return BadRequest(new { 
                    error = "Path contains invalid characters", 
                    sanitizedPath = sanitizedPath 
                });
            }

            // Check if path is allowed
            var isAllowed = await _securityService.IsPathAllowedAsync(sanitizedPath);
            if (!isAllowed)
            {
                _logger.LogWarning("Access denied to path: {Path}", sanitizedPath);
                return StatusCode(403, new { error = "Access to this path is not allowed" });
            }

            return Ok(new { 
                isValid = true, 
                sanitizedPath = sanitizedPath,
                message = "Path is valid and allowed" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating path: {Path}", request.Path);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("validate-input")]
    public IActionResult ValidateInput([FromBody] ValidateInputRequest request)
    {
        try
        {
            var result = new ValidateInputResponse
            {
                IsValid = true,
                SanitizedInput = request.Input,
                Issues = new List<string>()
            };

            // Check for SQL injection patterns
            if (ContainsSqlInjection(request.Input))
            {
                result.Issues.Add("Potential SQL injection detected");
                result.IsValid = false;
            }

            // Check for script injection
            if (ContainsScriptInjection(request.Input))
            {
                result.Issues.Add("Potential script injection detected");
                result.IsValid = false;
            }

            // Check for command injection
            if (ContainsCommandInjection(request.Input))
            {
                result.Issues.Add("Potential command injection detected");
                result.IsValid = false;
            }

            // Sanitize input if needed
            if (!result.IsValid)
            {
                result.SanitizedInput = SanitizeInput(request.Input);
            }

            if (!result.IsValid)
            {
                _logger.LogWarning("Invalid input detected: {Input}, Issues: {Issues}", 
                    request.Input, string.Join(", ", result.Issues));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating input");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("security-headers")]
    public IActionResult GetSecurityHeaders()
    {
        // Add security headers
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["X-XSS-Protection"] = "1; mode=block";
        Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:";

        return Ok(new { message = "Security headers set" });
    }

    private string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Remove dangerous characters and patterns
        var sanitized = path
            .Replace("..", "") // Prevent directory traversal
            .Replace("\\", "/") // Normalize path separators
            .Trim();

        // Remove leading slashes except for the first one
        sanitized = Regex.Replace(sanitized, @"^/+", "/");
        
        // Remove multiple consecutive slashes
        sanitized = Regex.Replace(sanitized, @"/+", "/");

        return sanitized;
    }

    private string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Basic HTML encoding
        return System.Net.WebUtility.HtmlEncode(input);
    }

    private bool ContainsSqlInjection(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var sqlPatterns = new[]
        {
            @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|CREATE|ALTER|EXEC|EXECUTE)\b)",
            @"('|('')|[^\w\s]*((\%27)|(\'))[^\w\s]*)",
            @"(((\%3D)|(=))[^\n]*((\%27)|(\')|(--|;)))",
            @"(\w*((\%27)|(\'))((\%6F)|o|(\%4F))((\%72)|r|(\%52)))",
        };

        return sqlPatterns.Any(pattern => 
            Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    private bool ContainsScriptInjection(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var scriptPatterns = new[]
        {
            @"<\s*script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
            @"javascript\s*:",
            @"on\w+\s*=",
            @"eval\s*\(",
            @"expression\s*\(",
        };

        return scriptPatterns.Any(pattern => 
            Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    private bool ContainsCommandInjection(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var commandPatterns = new[]
        {
            @"[;&|`]",
            @"(\$\(|\`)",
            @"(cmd|powershell|bash|sh)\s",
            @"(rm|del|format|mkfs)\s",
        };

        return commandPatterns.Any(pattern => 
            Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }
}

public class ValidatePathRequest
{
    public string Path { get; set; } = string.Empty;
}

public class ValidateInputRequest
{
    public string Input { get; set; } = string.Empty;
    public string InputType { get; set; } = "text";
}

public class ValidateInputResponse
{
    public bool IsValid { get; set; }
    public string SanitizedInput { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
}