using System.Security.Claims;
using api.Contracts.Chats;
using api.Controllers;
using api.Entities;
using api.Infrastructure.BackgroundJobs;
using api.Persistence;
using api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace api.Tests.Controllers;

public class ChatsControllerTests : IDisposable
{
    private readonly Mock<IChatService> _chatServiceMock = new();
    private readonly Mock<IBackgroundTaskQueue> _queueMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly ApplicationDbContext _db;
    private readonly ChatsController _sut;

    public ChatsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _queueMock
            .Setup(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()))
            .Returns(ValueTask.CompletedTask);

        _sut = new ChatsController(
            _chatServiceMock.Object,
            _db,
            _queueMock.Object,
            _scopeFactoryMock.Object,
            NullLogger<ChatsController>.Instance);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        SetupAnonymousUser();
        var result = await _sut.Create(null, CancellationToken.None);
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Create_UserNotFound_Returns404()
    {
        SetupAuthenticatedUser("999");
        var result = await _sut.Create(new CreateChatRequest(3), CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ValidUser_Returns201()
    {
        SeedUser(1);
        SetupAuthenticatedUser("1");

        _chatServiceMock
            .Setup(s => s.CreateChatAsync(1, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(10, "Welcome", ZeroScores(), ZeroScores(), "normal", [], DateTime.UtcNow));

        var result = await _sut.Create(new CreateChatRequest(3), CancellationToken.None) as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task SendMessage_ChatMissing_Returns404()
    {
        SetupAuthenticatedUser("1");
        _chatServiceMock
            .Setup(s => s.SendMessageAsync(1, 10, "hello", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("not found"));

        var result = await _sut.SendMessage(10, new SendChatMessageRequest("hello"), CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetHistory_InvalidPageSize_Returns400()
    {
        SetupAuthenticatedUser("1");
        var result = await _sut.GetHistory(1, 80, CancellationToken.None);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EndChat_NotFound_Returns404()
    {
        SetupAuthenticatedUser("1");
        _chatServiceMock
            .Setup(s => s.EndChatAsync(77, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.EndChat(77, CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EndChat_Success_QueuesSummaryAndReturns200()
    {
        SetupAuthenticatedUser("1");
        _chatServiceMock
            .Setup(s => s.EndChatAsync(77, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.EndChat(77, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _queueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Once);
    }

    private void SeedUser(int id)
    {
        _db.Users.Add(new User
        {
            Id = id,
            Username = $"u{id}",
            Email = $"u{id}@app.test",
            FirstName = "A",
            LastName = "B",
            PasswordHash = "hash"
        });
        _db.SaveChanges();
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim> { new("sub", userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
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

    private static Dictionary<string, int> ZeroScores() =>
        new()
        {
            ["ANX"] = 0, ["DEP"] = 0, ["STR"] = 0, ["SLP"] = 0,
            ["SOC"] = 0, ["CDT"] = 0, ["SAFE"] = 0, ["ENG"] = 0
        };

    public void Dispose() => _db.Dispose();
}
