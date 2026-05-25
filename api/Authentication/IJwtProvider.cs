using api.Entities;

namespace api.Authentication;

public interface IJwtProvider
{
    (string token, int expiresIn) GenerateToken(User user);

    /// <summary>
    /// Validates a JWT and returns the user's ID (sub claim) if valid; otherwise null.
    /// </summary>
    string? ValidateToken(string token);
}
