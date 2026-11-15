using System.ComponentModel.DataAnnotations;

namespace Sentinel.Application.Identity;

public sealed class RegisterRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record AuthResult(bool Succeeded, AuthResponse? Response, IReadOnlyList<string> Errors);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

