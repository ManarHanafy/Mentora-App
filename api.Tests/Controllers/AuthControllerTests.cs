using System.Security.Claims;
using api.Abstractions;
using api.Contracts.Authentication;
using api.Contracts.Users;
using api.Controllers;
using api.Errors;
using api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly AuthController     _sut;

    private static readonly AuthResponse SampleAuthResponse = new(
        "1", "user@example.com", "John", "Doe",
        "jwt-token", 3600,
        "refresh-token-abc", DateTime.UtcNow.AddDays(14));

    public AuthControllerTests()
    {
        _sut = new AuthController(_authServiceMock.Object, _userServiceMock.Object);
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var request = new LoginRequest("user@example.com", "password123");

        _authServiceMock
            .Setup(s => s.GetTokenAsync(request.Email, request.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(SampleAuthResponse));

        var result = await _sut.Login(request, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(SampleAuthResponse);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns400()
    {
        var request = new LoginRequest("bad@example.com", "wrongpassword");

        _authServiceMock
            .Setup(s => s.GetTokenAsync(request.Email, request.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponse>(UserErrors.InvalidCredentials));

        var result = await _sut.Login(request, CancellationToken.None) as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_EmptyEmail_Returns400()
    {
        var request = new LoginRequest("", "password");

        _authServiceMock
            .Setup(s => s.GetTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponse>(UserErrors.InvalidCredentials));

        var result = await _sut.Login(request, CancellationToken.None) as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var request = new CreateUserRequest("jdoe", "existing@example.com", "John", "Doe", "pass123");

        _userServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserResponse?)null, UserErrors.DuplicateEmail.Description));

        var result = await _sut.Register(request, CancellationToken.None) as ConflictObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(409);
    }

    // ── POST /api/auth/refresh ────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokenPair()
    {
        var request = new RefreshTokenRequest("old-jwt", "old-refresh");

        _authServiceMock
            .Setup(s => s.GetRefreshTokenAsync(request.Token, request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(SampleAuthResponse));

        var result = await _sut.Refresh(request, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(SampleAuthResponse);
    }

    [Fact]
    public async Task Refresh_InvalidRefreshToken_Returns401()
    {
        var request = new RefreshTokenRequest("old-jwt", "bad-refresh");

        _authServiceMock
            .Setup(s => s.GetRefreshTokenAsync(request.Token, request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponse>(UserErrors.InvalidRefreshToken));

        var result = await _sut.Refresh(request, CancellationToken.None) as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    // ── POST /api/auth/revoke-refresh-token ───────────────────────────────────

    [Fact]
    public async Task RevokeRefreshToken_ValidToken_Returns200()
    {
        var request = new RefreshTokenRequest("valid-jwt", "valid-refresh");

        _authServiceMock
            .Setup(s => s.RevokeRefreshTokenAsync(request.Token, request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.RevokeRefreshToken(request, CancellationToken.None) as OkResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RevokeRefreshToken_InvalidToken_Returns401()
    {
        var request = new RefreshTokenRequest("bad-jwt", "bad-refresh");

        _authServiceMock
            .Setup(s => s.RevokeRefreshTokenAsync(request.Token, request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(UserErrors.InvalidJwtToken));

        var result = await _sut.RevokeRefreshToken(request, CancellationToken.None) as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    // ── GET /api/auth/status ──────────────────────────────────────────────────

    [Fact]
    public void GetStatus_AuthenticatedUser_Returns200WithClaims()
    {
        SetupAuthenticatedUser("42", "test@example.com");

        var result = _sut.GetStatus() as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public void GetStatus_AnonymousUser_Returns200WithFalseAuthenticated()
    {
        SetupAnonymousUser();

        var result = _sut.GetStatus() as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    // ── GET /api/auth/me ──────────────────────────────────────────────────────

    [Fact]
    public void GetMe_AuthenticatedUser_Returns200WithUserInfo()
    {
        SetupAuthenticatedUser("42", "me@example.com", "Jane", "Smith");

        var result = _sut.GetMe() as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupAuthenticatedUser(string userId, string email, string firstName = "First", string lastName = "Last")
    {
        var claims = new List<Claim>
        {
            new("sub",         userId),
            new("email",       email),
            new("given_name",  firstName),
            new("family_name", lastName),
        };
        var identity  = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupAnonymousUser()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }
}
