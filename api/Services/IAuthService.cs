using api.Contracts.Authentication;
using api.Abstractions;

namespace api.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> GetTokenAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> GetRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default);
    Task<Result>               RevokeRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default);
    Task<Result>               VerifyEmailOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
    Task<Result>               ResendEmailOtpAsync(string email, CancellationToken cancellationToken = default);
    Task<Result>               RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<Result>               ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<Result>               LogoutAsync(int userId, CancellationToken cancellationToken = default);
}
