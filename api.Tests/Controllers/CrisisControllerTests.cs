using System.Security.Claims;
using api.Controllers;
using api.Contracts.Crisis;
using api.Entities;
using api.Persistence;
using api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.Controllers;

public class CrisisControllerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly CrisisController _sut;

    public CrisisControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _sut = new CrisisController(new StubCrisisResourceService());
    }

    [Fact]
    public async Task CheckUserCrisisStatus_InvalidTokenClaims_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.CheckUserCrisisStatus(1, _db, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task CheckUserCrisisStatus_DifferentUser_Returns403()
    {
        SetupAuthenticatedUser("1");

        var result = await _sut.CheckUserCrisisStatus(2, _db, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CheckUserCrisisStatus_OwnerWithJournal_Returns200()
    {
        SetupAuthenticatedUser("5");
        _db.JournalEntries.Add(new JournalEntry
        {
            UserId = 5,
            JournalText = "Need help",
            RiskLevel = "crisis",
            AiResponseJson = "{}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CheckUserCrisisStatus(5, _db, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var identity = new ClaimsIdentity([new Claim("sub", userId)], "Bearer");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
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

    private sealed class StubCrisisResourceService : ICrisisResourceService
    {
        public CrisisResourcesResponse GetResources(string? locale, string? countryCode) =>
            new(
                Message: "stub",
                Resources: [new CrisisResource("resource", "type", "contact", "description", true)],
                ImmediateAdvice: "advice");
    }
}
