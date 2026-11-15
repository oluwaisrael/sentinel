using System.ComponentModel.DataAnnotations;

namespace Sentinel.Application.Identity;

public sealed record RegisterRequest(
    [property: Required, EmailAddress, StringLength(256)] string Email,
    [property: Required, StringLength(128, MinimumLength = 12)] string Password);

public sealed record LoginRequest(
    [property: Required, EmailAddress, StringLength(256)] string Email,
    [property: Required] string Password);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record AuthResult(bool Succeeded, AuthResponse? Response, IReadOnlyList<string> Errors);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

