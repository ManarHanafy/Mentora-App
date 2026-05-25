using System.Security.Cryptography;
using System.Text;
using api.Contracts.Authentication;
using api.Persistence;
using api.Authentication;
using api.Infrastructure.Audit;
using api.Infrastructure.Email;
using api.Abstractions;
using api.Errors;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class AuthService(
    ApplicationDbContext db,
    IJwtProvider         jwtProvider,
    ILogger<AuthService> logger,
    IAuditLogger         auditLogger,
    IEmailSender         emailSender) : IAuthService
{
    private const int RefreshTokenExpiryDays = 14;
    private const int OtpLength = 5;
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ResetTokenExpiry = TimeSpan.FromMinutes(15);
    private const int MaxFailedLogins = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> GetTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            await auditLogger.LogAuthorizationFailureAsync(0, "Login", "Authenticate", "Unknown email");
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
        }

        if (!user.IsActive)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        if (user.LockoutUntil is not null && user.LockoutUntil > DateTime.UtcNow)
            return Result.Failure<AuthResponse>(UserErrors.AccountLocked);

        if (!user.EmailVerified)
            return Result.Failure<AuthResponse>(UserErrors.EmailNotVerified);

        var validPassword = !string.IsNullOrEmpty(user.PasswordHash)
            && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!validPassword)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedLogins)
            {
                user.LockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
            }

            await db.SaveChangesAsync(cancellationToken);
            await auditLogger.LogAuthorizationFailureAsync(user.Id, "Login", "Authenticate", "Invalid password");
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
        }

        var (token, expiresIn) = jwtProvider.GenerateToken(user);
        var refreshToken       = GenerateRefreshToken();
        var refreshExpiry      = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        user.RefreshTokens.Add(new Entities.RefreshToken
        {
            Token     = HashToken(refreshToken),
            ExpiresOn = refreshExpiry
        });

        user.LastLogin = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} authenticated successfully", user.Id);
        await auditLogger.LogSensitiveAccessAsync(user.Id, "Login", user.Id);

        return Result.Success(new AuthResponse(
            user.Id, user.Email,
            user.FirstName,     user.LastName,
            token,              expiresIn,
            refreshToken,       refreshExpiry));
    }

    public async Task<Result> VerifyEmailOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (user.EmailVerified)
            return Result.Success();

        if (user.EmailOtpExpiresAt is null || user.EmailOtpExpiresAt <= DateTime.UtcNow)
            return Result.Failure(UserErrors.OtpExpired);

        if (string.IsNullOrWhiteSpace(user.EmailOtpHash))
            return Result.Failure(UserErrors.OtpInvalid);

        var isValid = BCrypt.Net.BCrypt.Verify(otp, user.EmailOtpHash);
        if (!isValid)
            return Result.Failure(UserErrors.OtpInvalid);

        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailOtpHash = null;
        user.EmailOtpExpiresAt = null;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Email verified for user {UserId}", user.Id);
        return Result.Success();
    }

    public async Task<Result> ResendEmailOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (user.EmailVerified)
            return Result.Success();

        var otp = GenerateOtpCode();
        user.EmailOtpHash = BCrypt.Net.BCrypt.HashPassword(otp);
        user.EmailOtpExpiresAt = DateTime.UtcNow.Add(OtpExpiry);

        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendEmailAsync(
                user.Email,
                "Verify your email",
                EmailTemplateBuilder.BuildOtpBody(otp, (int)OtpExpiry.TotalMinutes),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend verification email for {Email}", user.Email);
            return Result.Failure(UserErrors.OtpInvalid);
        }

        logger.LogInformation("OTP resent for user {UserId}", user.Id);
        return Result.Success();
    }

    public async Task<Result> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null)
            return Result.Success();

        var token = GenerateResetToken();
        var resetToken = new Entities.PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(token),
            ExpiresAt = DateTime.UtcNow.Add(ResetTokenExpiry)
        };

        db.PasswordResetTokens.Add(resetToken);

        try
        {
            await emailSender.SendEmailAsync(
                user.Email,
                "Reset your password",
                EmailTemplateBuilder.BuildPasswordResetBody(token, (int)ResetTokenExpiry.TotalMinutes),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email for {Email}", user.Email);
            return Result.Failure(UserErrors.ResetTokenInvalid);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password reset token generated for user {UserId}", user.Id);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var candidateTokens = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var matched = candidateTokens.FirstOrDefault(t => BCrypt.Net.BCrypt.Verify(token, t.TokenHash));
        if (matched is null)
            return Result.Failure(UserErrors.ResetTokenInvalid);

        if (matched.ExpiresAt <= DateTime.UtcNow)
            return Result.Failure(UserErrors.ResetTokenExpired);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        matched.UsedAt = DateTime.UtcNow;

        // Revoke all active refresh tokens on password reset
        foreach (var rt in user.RefreshTokens.Where(t => t.IsActive))
            rt.RevokedOn = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password reset for user {UserId}", user.Id);
        await auditLogger.LogSensitiveAccessAsync(user.Id, "PasswordReset", user.Id);
        return Result.Success();
    }

    // ── Refresh token ─────────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> GetRefreshTokenAsync(
        string token,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var userId = jwtProvider.ValidateToken(token);
        if (userId is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

        if (!int.TryParse(userId, out var userIdInt))
            return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userIdInt, cancellationToken);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

        var hashedToken = HashToken(refreshToken);
        var userRefreshToken = user.RefreshTokens
            .SingleOrDefault(rt => rt.IsActive && (rt.Token == hashedToken || rt.Token == refreshToken));

        if (userRefreshToken is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidRefreshToken);

        if (string.Equals(userRefreshToken.Token, refreshToken, StringComparison.Ordinal))
            logger.LogWarning("Legacy plaintext refresh token matched for user {UserId}.", user.Id);

        // Revoke old token
        userRefreshToken.RevokedOn = DateTime.UtcNow;

        // Issue new pair
        var (newToken, expiresIn) = jwtProvider.GenerateToken(user);
        var newRefreshToken       = GenerateRefreshToken();
        var refreshExpiry         = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        user.RefreshTokens.Add(new Entities.RefreshToken
        {
            Token     = HashToken(newRefreshToken),
            ExpiresOn = refreshExpiry
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);

        return Result.Success(new AuthResponse(
            user.Id, user.Email,
            user.FirstName,     user.LastName,
            newToken,           expiresIn,
            newRefreshToken,    refreshExpiry));
    }

    // ── Revoke refresh token ──────────────────────────────────────────────────

    public async Task<Result> RevokeRefreshTokenAsync(
        string token,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var userId = jwtProvider.ValidateToken(token);
        if (userId is null)
            return Result.Failure(UserErrors.InvalidJwtToken);

        if (!int.TryParse(userId, out var userIdInt))
            return Result.Failure(UserErrors.InvalidJwtToken);

        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userIdInt, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.InvalidJwtToken);

        var hashedToken = HashToken(refreshToken);
        var userRefreshToken = user.RefreshTokens
            .SingleOrDefault(rt => rt.IsActive && (rt.Token == hashedToken || rt.Token == refreshToken));

        if (userRefreshToken is null)
            return Result.Failure(UserErrors.InvalidRefreshToken);

        if (string.Equals(userRefreshToken.Token, refreshToken, StringComparison.Ordinal))
            logger.LogWarning("Legacy plaintext refresh token matched for user {UserId}.", user.Id);

        userRefreshToken.RevokedOn = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Refresh token revoked for user {UserId}", user.Id);
        await auditLogger.LogSensitiveAccessAsync(userIdInt, "RefreshTokenRevoke", userIdInt);
        return Result.Success();
    }

    public async Task<Result> LogoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
            token.RevokedOn = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} logged out", user.Id);
        return Result.Success();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <summary>
    /// Hashes a refresh token with SHA-256 for secure storage.
    /// The plaintext token is sent to the client; only the hash is persisted.
    /// </summary>
    internal static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string GenerateOtpCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(OtpLength);
        var digits = bytes.Select(b => (b % 10).ToString()); // Convert bytes to digits
        return string.Concat(digits);
    }

    private static string GenerateResetToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
