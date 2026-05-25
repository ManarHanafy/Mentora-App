using api.Abstractions;
using api.Authentication;
using api.Contracts.Authentication;
using api.Entities;
using api.Errors;
using api.Infrastructure.Email;
using api.Persistence;
using api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Tests;

public class AuthServiceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static User CreateUser(string email, string passwordHash)
    {
        return new User
        {
            Email = email,
            Username = "tester",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = passwordHash,
            EmailVerified = true,
            IsActive = true
        };
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsFailure_WhenEmailNotVerified()
    {
        await using var db = CreateDbContext(nameof(GetTokenAsync_ReturnsFailure_WhenEmailNotVerified));
        var user = CreateUser("user@example.com", BCrypt.Net.BCrypt.HashPassword("Password123"));
        user.EmailVerified = false;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwtProvider = new Mock<IJwtProvider>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<AuthService>>();

        var service = new AuthService(db, jwtProvider.Object, logger.Object, emailSender.Object);

        var result = await service.GetTokenAsync(user.Email, "Password123");

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailNotVerified, result.Error);
    }

    [Fact]
    public async Task GetTokenAsync_LocksAccount_AfterMaxFailedAttempts()
    {
        await using var db = CreateDbContext(nameof(GetTokenAsync_LocksAccount_AfterMaxFailedAttempts));
        var user = CreateUser("user2@example.com", BCrypt.Net.BCrypt.HashPassword("Password123"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwtProvider = new Mock<IJwtProvider>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<AuthService>>();

        var service = new AuthService(db, jwtProvider.Object, logger.Object, emailSender.Object);

        for (var i = 0; i < 5; i++)
            await service.GetTokenAsync(user.Email, "WrongPassword");

        var updatedUser = await db.Users.SingleAsync(u => u.Email == user.Email);
        Assert.NotNull(updatedUser.LockoutUntil);
        Assert.True(updatedUser.LockoutUntil > DateTime.UtcNow);
        Assert.Equal(0, updatedUser.FailedLoginCount);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ReturnsFailure_WhenRefreshTokenIsInvalid()
    {
        await using var db = CreateDbContext(nameof(GetRefreshTokenAsync_ReturnsFailure_WhenRefreshTokenIsInvalid));
        var user = CreateUser("user3@example.com", BCrypt.Net.BCrypt.HashPassword("Password123"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwtProvider = new Mock<IJwtProvider>();
        jwtProvider.Setup(x => x.ValidateToken(It.IsAny<string>())).Returns(user.Id.ToString());

        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(db, jwtProvider.Object, logger.Object, emailSender.Object);

        var result = await service.GetRefreshTokenAsync("token", "missing-refresh-token");

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFailure_WhenTokenExpired()
    {
        await using var db = CreateDbContext(nameof(ResetPasswordAsync_ReturnsFailure_WhenTokenExpired));
        var user = CreateUser("user4@example.com", BCrypt.Net.BCrypt.HashPassword("Password123"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var expiredToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword("reset-token"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        db.PasswordResetTokens.Add(expiredToken);
        await db.SaveChangesAsync();

        var jwtProvider = new Mock<IJwtProvider>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(db, jwtProvider.Object, logger.Object, emailSender.Object);

        var result = await service.ResetPasswordAsync(user.Email, "reset-token", "NewPass123");

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.ResetTokenExpired, result.Error);
    }

    [Fact]
    public async Task VerifyEmailOtpAsync_ReturnsFailure_WhenOtpExpired()
    {
        await using var db = CreateDbContext(nameof(VerifyEmailOtpAsync_ReturnsFailure_WhenOtpExpired));
        var user = CreateUser("user5@example.com", BCrypt.Net.BCrypt.HashPassword("Password123"));
        user.EmailVerified = false;
        user.EmailOtpHash = BCrypt.Net.BCrypt.HashPassword("12345");
        user.EmailOtpExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwtProvider = new Mock<IJwtProvider>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(db, jwtProvider.Object, logger.Object, emailSender.Object);

        var result = await service.VerifyEmailOtpAsync(user.Email, "12345");

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.OtpExpired, result.Error);
    }
}
