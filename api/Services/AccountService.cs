using Mapster;
using Microsoft.EntityFrameworkCore;
using api.Abstractions;
using api.Contracts.Account;
using api.Contracts.Users;
using api.Errors;
using api.Infrastructure.Audit;
using api.Infrastructure.Caching;
using api.Persistence;

namespace api.Services;

public class AccountService(
    ApplicationDbContext db,
    IAppCacheService cache,
    IAuditLogger auditLogger,
    ILogger<AccountService> logger) : IAccountService
{
    public async Task<Result<UserResponse>> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.ParameterSnapshot)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result.Failure<UserResponse>(UserErrors.NotFound);

        user.FirstName = request.FirstName.Trim();
        user.LastName  = request.LastName.Trim();
        var normalizedUsername = request.Username.Trim();
        if (!string.Equals(user.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase))
        {
            var usernameExists = await db.Users
                .AnyAsync(u => u.Id != userId && u.Username == normalizedUsername, cancellationToken);
            if (usernameExists)
                return Result.Failure<UserResponse>(UserErrors.DuplicateUsername);
        }

        user.Username = normalizedUsername;

        await db.SaveChangesAsync(cancellationToken);
        InvalidateUserCache(userId);
        logger.LogInformation("Profile updated for user {UserId}", userId);

        var count    = await db.JournalEntries.CountAsync(e => e.UserId == userId, cancellationToken);
        var response = user.Adapt<UserResponse>() with { TotalJournalEntries = count };
        return Result.Success(response);
    }

    public async Task<Result> ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var valid = !string.IsNullOrEmpty(user.PasswordHash)
                 && BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);

        if (!valid)
        {
            await auditLogger.LogAuthorizationFailureAsync(userId, "ChangePassword", "Authenticate", "Wrong current password");
            return Result.Failure(UserErrors.WrongPassword);
        }

        user.PasswordHash      = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.FailedLoginCount  = 0;
        user.LockoutUntil      = null;

        // Revoke all active refresh tokens on password change
        foreach (var rt in user.RefreshTokens.Where(t => t.IsActive))
            rt.RevokedOn = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        InvalidateUserCache(userId);
        logger.LogInformation("Password changed for user {UserId}", userId);
        await auditLogger.LogSensitiveAccessAsync(userId, "PasswordChange", userId);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        user.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        InvalidateUserCache(userId);
        logger.LogInformation("User {UserId} deactivated", userId);
        return Result.Success();
    }

    private void InvalidateUserCache(int userId)
    {
        cache.RemoveMany($"users:{userId}", $"users:{userId}:parameters");
    }
}
