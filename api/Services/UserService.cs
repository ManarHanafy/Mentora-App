using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using api.Abstractions;
using api.Authorization;
using api.Contracts.Common;
using api.Persistence;
using api.Entities;
using api.Contracts.Users;
using api.Errors;
using api.Infrastructure.Caching;
using api.Infrastructure.Email;

namespace api.Services;

public class UserService(
    ApplicationDbContext db,
    IAppCacheService cache,
    ILogger<UserService> logger,
    IEmailSender emailSender) : IUserService
{
    private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(5);
    private const int OtpLength = 5;
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(3);

    public async Task<(UserResponse? response, string? error)> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var email        = request.Email.Trim().ToLowerInvariant();
            var username     = request.Username.Trim();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var emailExists = await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
            if (emailExists)
                return (null, UserErrors.DuplicateEmail.Description);

            var usernameExists = await db.Users.AnyAsync(u => u.Username == username, cancellationToken);
            if (usernameExists)
                return (null, UserErrors.DuplicateUsername.Description);

            var otp = GenerateOtpCode();

            var user = new User
            {
                Username          = username,
                Email             = email,
                FirstName         = request.FirstName.Trim(),
                LastName          = request.LastName.Trim(),
                PasswordHash      = passwordHash,
                Role              = ApplicationRoles.User,
                CreatedAt         = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow,
                EmailVerified     = false,
                EmailOtpHash      = BCrypt.Net.BCrypt.HashPassword(otp),
                EmailOtpExpiresAt = DateTime.UtcNow.Add(OtpExpiry),
                ParameterSnapshot = new UserParameterSnapshot
                {
                    UpdatedAt = DateTime.UtcNow
                }
            };

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
                logger.LogError(ex, "Failed to send verification email for {Email}", user.Email);
                return (null, "Failed to send verification email. Please try again.");
            }

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            var response = user.Adapt<UserResponse>() with { TotalJournalEntries = 0 };
            InvalidateUserCache(user.Id);

            logger.LogInformation("User {UserId} created successfully and OTP sent", user.Id);
            return (response, null);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "User creation cancelled");
            return (null, "Request was cancelled.");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error creating user");
            return (null, "A database error occurred while creating the user.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating user");
            return (null, "An unexpected error occurred while creating the user.");
        }
    }

    public async Task<UserResponse?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            GetUserByIdCacheKey(userId),
            async () =>
            {
                var user = await db.Users
                    .Include(u => u.ParameterSnapshot)
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (user is null)
                    return null;

                var count = await db.JournalEntries.CountAsync(e => e.UserId == userId, cancellationToken);
                var response = user.Adapt<UserResponse>() with { TotalJournalEntries = count };
                logger.LogInformation("Retrieved user {UserId}", userId);
                return response;
            },
            UserCacheTtl,
            cancellationToken);
    }

    public async Task<UserResponse?> GetByIdForAdminAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.ParameterSnapshot)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        var count = await db.JournalEntries.CountAsync(e => e.UserId == userId, cancellationToken);
        var response = user.Adapt<UserResponse>() with { TotalJournalEntries = count };
        logger.LogInformation("Retrieved user {UserId} for admin", userId);
        return response;
    }

    public async Task<PagedResponse<UserResponse>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = db.Users
            .Include(u => u.ParameterSnapshot)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Email.ToLower().Contains(search) ||
                u.Username.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        if (request.IsActive is not null)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (request.EmailVerified is not null)
            query = query.Where(u => u.EmailVerified == request.EmailVerified.Value);

        var isDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortBy?.Trim().ToLowerInvariant() switch
        {
            "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "username" => isDesc ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "createdat" => isDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            "lastlogin" => isDesc ? query.OrderByDescending(u => u.LastLogin) : query.OrderBy(u => u.LastLogin),
            _ => query.OrderBy(u => u.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var pageItems = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var pageUserIds = pageItems.Select(u => u.Id).ToList();
        var journalCounts = await db.JournalEntries
            .Where(e => pageUserIds.Contains(e.UserId))
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var result = new List<UserResponse>(pageItems.Count);
        foreach (var user in pageItems)
        {
            var count    = journalCounts.GetValueOrDefault(user.Id, 0);
            var response = user.Adapt<UserResponse>() with { TotalJournalEntries = count };
            result.Add(response);
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        logger.LogInformation("Retrieved users page {PageNumber} with {Count} users", request.PageNumber, result.Count);

        return new PagedResponse<UserResponse>(
            result,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages,
            request.PageNumber < totalPages,
            request.PageNumber > 1);
    }

    public async Task<UserParametersResponse?> GetParametersAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            GetUserParametersCacheKey(userId),
            async () =>
            {
                var snapshot = await db.UserParameterSnapshots
                    .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

                if (snapshot is null)
                    return null;

                return new UserParametersResponse(
                    snapshot.UserId,
                    new ParameterValues(snapshot.Anx, snapshot.Dep, snapshot.Str, snapshot.Slp,
                                        snapshot.Soc, snapshot.Cdt, snapshot.Safe, snapshot.Eng),
                    snapshot.UpdatedAt,
                    snapshot.LatestJournalEntryId
                );
            },
            UserCacheTtl,
            cancellationToken);
    }

    public async Task<Result> UpdateRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        var normalizedRole = ApplicationRoles.All
            .FirstOrDefault(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

        if (normalizedRole is null)
            return Result.Failure(UserErrors.InvalidRole);

        var user = await db.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        user.Role = normalizedRole;
        await db.SaveChangesAsync(cancellationToken);
        InvalidateUserCache(userId);
        logger.LogInformation("Updated role for user {UserId} to {Role}", userId, normalizedRole);
        return Result.Success();
    }

    public async Task<Result> UpdateStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        user.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        InvalidateUserCache(userId);
        logger.LogInformation("Updated status for user {UserId} to {Status}", userId, isActive);
        return Result.Success();
    }

    private void InvalidateUserCache(int userId)
    {
        cache.RemoveMany(GetUserByIdCacheKey(userId), GetUserParametersCacheKey(userId));
    }

    private static string GetUserByIdCacheKey(int userId) => $"users:{userId}";
    private static string GetUserParametersCacheKey(int userId) => $"users:{userId}:parameters";

    private static string GenerateOtpCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(OtpLength);
        var digits = bytes.Select(b => (b % 10).ToString());
        return string.Concat(digits);
    }
}
