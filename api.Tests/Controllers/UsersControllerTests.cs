using System.Security.Claims;
using api.Contracts.Users;
using api.Controllers;
using api.Errors;
using api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace api.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly UsersController    _sut;

    private static readonly UserResponse SampleUser = new(
        1, "jdoe", "john@example.com", "John", "Doe", "User",
        DateTime.UtcNow, 5, new Dictionary<string, int> { { "anx", 3 } }, DateTime.UtcNow);

    public UsersControllerTests()
    {
        _sut = new UsersController(_userServiceMock.Object);
    }

    // ── GET /api/users/me ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_ValidToken_Returns200WithUser()
    {
        SetupAuthenticatedUser("1");
        _userServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(SampleUser);

        var result = await _sut.GetMe(CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(SampleUser);
    }

    [Fact]
    public async Task GetMe_UserNotFound_Returns404()
    {
        SetupAuthenticatedUser("99");
        _userServiceMock.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((UserResponse?)null);

        var result = await _sut.GetMe(CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetMe_InvalidToken_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.GetMe(CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    // ── POST /api/users ───────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithUser()
    {
        var request = new CreateUserRequest("jdoe", "john@example.com", "John", "Doe", "pass123");

        _userServiceMock.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((SampleUser, (string?)null));

        var result = await _sut.Create(request, CancellationToken.None) as CreatedAtActionResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        result.Value.Should().BeEquivalentTo(SampleUser);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409WithError()
    {
        var request = new CreateUserRequest("jdoe", "existing@example.com", "John", "Doe", "pass123");

        _userServiceMock.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(((UserResponse?)null, UserErrors.DuplicateEmail.Description));

        var result = await _sut.Create(request, CancellationToken.None) as ConflictObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(409);
    }

    // ── GET /api/users/parameters ─────────────────────────────────────────────

    [Fact]
    public async Task GetParameters_AuthenticatedUser_Returns200()
    {
        SetupAuthenticatedUser("1");
        var parameters = new UserParametersResponse(
            1,
            new ParameterValues(2, 3, 1, 5, 0, 1, 0, 7),
            DateTime.UtcNow,
            42
        );

        _userServiceMock.Setup(s => s.GetParametersAsync(1, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(parameters);

        var result = await _sut.GetParameters(CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public async Task GetParameters_UserNotFound_Returns404()
    {
        SetupAuthenticatedUser("999");
        _userServiceMock.Setup(s => s.GetParametersAsync(999, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((UserParametersResponse?)null);

        var result = await _sut.GetParameters(CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetParameters_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.GetParameters(CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupAuthenticatedUser(string userId)
    {
        var identity  = new ClaimsIdentity(new[] { new Claim("sub", userId) }, "Bearer");
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
