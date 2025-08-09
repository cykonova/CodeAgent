namespace CodeAgent.Gateway.Models;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName
);

public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record AuthResponse
{
    public required string Token { get; init; }
    public required string RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public UserDto? User { get; init; }
}

public record UserDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string[]? Roles { get; init; }
}