using System.Security.Claims;
using api.Contracts.Journals;
using api.Controllers;
using api.Contracts.Common;
using api.Entities;
using api.Persistence;
using api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace api.Tests.Controllers;

public class JournalsControllerTests : IDisposable
{
    private readonly Mock<IJournalService>  _journalServiceMock = new();
    private readonly ApplicationDbContext   _db;
    private readonly JournalsController     _sut;

    private static readonly JournalResponse SampleJournalResponse = new(
        MatchedItems: new List<api.Contracts.AI.MatchedItemResponse>(),
        Deltas: new Dictionary<string, int> { { "ANX", 2 } },
        NewScores: new Dictionary<string, int> { { "ANX", 5 } },
        Tags: new List<string> { "anxiety" },
        RiskLevel: "elevated",
        SuggestedExercises: new List<api.Contracts.Exercises.SuggestedExerciseResponse>());

    public JournalsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db  = new ApplicationDbContext(options);
        _sut = new JournalsController(_journalServiceMock.Object, _db);
    }

    // ── POST /api/journals ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        SeedUser(1);
        SetupAuthenticatedUser("1");
        var request = new SubmitJournalRequest("I feel anxious today.");

        _journalServiceMock
            .Setup(s => s.SubmitAsync(1, It.IsAny<SubmitJournalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleJournalResponse);

        var result = await _sut.Create(request, CancellationToken.None) as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        result.Value.Should().BeEquivalentTo(SampleJournalResponse);
    }

    [Fact]
    public async Task Create_UserNotFound_Returns404()
    {
        SeedUser(1);
        SetupAuthenticatedUser("999"); // userId 999 doesn't exist
        var request = new SubmitJournalRequest("Some content.");

        var result = await _sut.Create(request, CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_InvalidToken_Returns401()
    {
        SetupAnonymousUser();
        var request = new SubmitJournalRequest("Some content.");

        var result = await _sut.Create(request, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    // ── GET /api/journals ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_AuthenticatedUser_Returns200WithList()
    {
        SetupAuthenticatedUser("1");
        var history = new PagedResponse<JournalSummaryResponse>(
            new List<JournalSummaryResponse>
            {
                new(1, 1, "normal", Array.Empty<string>(), DateTime.UtcNow)
            },
            PageNumber: 1,
            PageSize: 20,
            TotalCount: 1,
            TotalPages: 1,
            HasNextPage: false,
            HasPreviousPage: false);

        _journalServiceMock
            .Setup(s => s.GetHistoryAsync(1, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = await _sut.GetHistory(1, 20, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetHistory_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.GetHistory(1, 20, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetHistory_AuthenticatedUserNoEntries_Returns200EmptyList()
    {
        SetupAuthenticatedUser("5");
        _journalServiceMock
            .Setup(s => s.GetHistoryAsync(5, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<JournalSummaryResponse>(
                Items: [],
                PageNumber: 1,
                PageSize: 20,
                TotalCount: 0,
                TotalPages: 0,
                HasNextPage: false,
                HasPreviousPage: false));

        var result = await _sut.GetHistory(1, 20, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    // ── GET /api/journals/trend ────────────────────────────────────────────────

    [Fact]
    public async Task GetTrend_AuthenticatedUser_Returns200()
    {
        SetupAuthenticatedUser("1");

        var result = await _sut.GetTrend(10, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetTrend_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.GetTrend(10, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetTrend_InvalidLimit_Returns400()
    {
        SetupAuthenticatedUser("1");

        var result = await _sut.GetTrend(0, CancellationToken.None) as BadRequestObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetTrend_LimitTooHigh_Returns400()
    {
        SetupAuthenticatedUser("1");

        var result = await _sut.GetTrend(51, CancellationToken.None) as BadRequestObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    // ── GET /api/journals/parameters ──────────────────────────────────────────

    [Fact]
    public async Task GetUserParameterSnapshots_AuthenticatedUser_Returns200()
    {
        SetupAuthenticatedUser("1");

        var result = await _sut.GetUserParameterSnapshots(10, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetUserParameterSnapshots_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.GetUserParameterSnapshots(10, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetUserParameterSnapshots_InvalidLimit_Returns400()
    {
        SetupAuthenticatedUser("1");

        var result = await _sut.GetUserParameterSnapshots(0, CancellationToken.None) as BadRequestObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    // ── PUT /api/journals/{journalId} ─────────────────────────────────────────

    [Fact]
    public async Task Update_ValidOwnership_Returns200()
    {
        SetupAuthenticatedUser("1");
        SeedJournal(1, 1);
        var request = new UpdateJournalRequest("Updated content about feeling better.");

        _journalServiceMock
            .Setup(s => s.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleJournalResponse);

        var result = await _sut.Update(1, request, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Update_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();
        var request = new UpdateJournalRequest("Content.");

        var result = await _sut.Update(1, request, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Update_WrongUser_Returns403()
    {
        SetupAuthenticatedUser("1");
        SeedJournal(1, 2); // journal belongs to user 2

        var request = new UpdateJournalRequest("Content.");

        var result = await _sut.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Update_JournalNotFound_Returns404()
    {
        SetupAuthenticatedUser("1");
        var request = new UpdateJournalRequest("Content.");

        var result = await _sut.Update(999, request, CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    // ── DELETE /api/journals/{journalId} ──────────────────────────────────────

    [Fact]
    public async Task Delete_ValidOwnership_Returns204()
    {
        SetupAuthenticatedUser("1");
        SeedJournal(1, 1);

        _journalServiceMock
            .Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.Delete(1, CancellationToken.None) as NoContentResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.Delete(1, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Delete_WrongUser_Returns403()
    {
        SetupAuthenticatedUser("1");
        SeedJournal(1, 2); // journal belongs to user 2

        var result = await _sut.Delete(1, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Delete_JournalNotFound_Returns404()
    {
        SetupAuthenticatedUser("1");

        var result = await _sut.Delete(999, CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SeedUser(int id)
    {
        if (_db.Users.Any(u => u.Id == id)) return;

        _db.Users.Add(new User
        {
            Id           = id,
            Username     = $"user{id}",
            Email        = $"user{id}@example.com",
            FirstName    = "Test",
            LastName     = "User",
            PasswordHash = "hash",
            CreatedAt    = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    private void SeedJournal(int journalId, int userId)
    {
        _db.JournalEntries.Add(new JournalEntry
        {
            Id        = journalId,
            UserId    = userId,
            JournalText   = "Test content.",
            RiskLevel = "normal",
            AiResponseJson = "{}"
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

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
