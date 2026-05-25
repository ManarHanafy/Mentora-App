using System.Security.Claims;
using api.Contracts.Onboarding;
using api.Contracts.Users;
using api.Controllers;
using api.Entities;
using api.Persistence;
using api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace api.Tests.Controllers;

public class OnboardingControllerTests : IDisposable
{
    private readonly Mock<IOnboardingService> _service = new();
    private readonly ApplicationDbContext _db;
    private readonly OnboardingController _sut;

    private static readonly OnboardingQuestionsResponse SampleQuestions = new(
        Completed: false,
        CompletedAt: null,
        ShouldShow: true,
        Locale: "en",
        Questions: new List<OnboardingQuestionResponse>());

    private static readonly OnboardingSubmitResponse SampleSubmitResponse = new(
        Success: true,
        Completed: true,
        CompletedAt: DateTime.UtcNow,
        Parameters: new ParameterValues(0, 0, 0, 0, 0, 0, 0, 0),
        Actions: []);

    public OnboardingControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new OnboardingController(_service.Object, _db);
    }

    [Fact]
    public async Task GetQuestions_Unauthenticated_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.GetQuestions(null, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Submit_ValidRequest_Returns200()
    {
        SeedUser(1);
        SetupAuthenticatedUser("1");

        var request = new SubmitOnboardingRequest([new OnboardingAnswerRequest(1, [1])]);
        _service.Setup(s => s.SubmitAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleSubmitResponse);

        var result = await _sut.Submit(request, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(SampleSubmitResponse);
    }

    [Fact]
    public async Task Submit_AlreadyCompleted_Returns409()
    {
        SeedUser(2);
        SetupAuthenticatedUser("2");
        var request = new SubmitOnboardingRequest([new OnboardingAnswerRequest(1, [1])]);

        _service.Setup(s => s.SubmitAsync(2, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Onboarding has already been completed."));

        var result = await _sut.Submit(request, CancellationToken.None) as ConflictObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Reset_WhenUserMissing_Returns404()
    {
        SetupAuthenticatedUser("99");
        _service.Setup(s => s.ResetAsync(99, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.Reset(5, CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    private void SeedUser(int id)
    {
        if (_db.Users.Any(u => u.Id == id)) return;

        _db.Users.Add(new User
        {
            Id = id,
            Username = $"user{id}",
            Email = $"user{id}@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId) }, "Bearer");
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

    public void Dispose() => _db.Dispose();
}
