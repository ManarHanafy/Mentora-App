using api.Contracts.Exercises;
using api.Controllers;
using api.Extensions;
using api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace api.Tests.Controllers;

public class ExercisesControllerTests
{
    private readonly Mock<IExerciseService> _exerciseServiceMock = new();
    private readonly ExercisesController    _sut;

    private static readonly List<ExerciseResponse> SampleExercises =
    [
        new(1, "EX_ANX_01", "anx", 6, "6-8", 10),
        new(2, "EX_DEP_02", "dep", 5, "4-6", 11),
        new(3, "EX_SLP_03", "slp", 7, "7-10", 12),
    ];

    public ExercisesControllerTests()
    {
        _sut = new ExercisesController(_exerciseServiceMock.Object);
    }

    // ── GET /api/exercises ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoFilter_Returns200WithAllExercises()
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.GetAllAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleExercises);

        var result = await _sut.GetAll(null, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(SampleExercises);
    }

    [Fact]
    public async Task GetAll_ValidParameterFilter_Returns200WithFilteredExercises()
    {
        SetupAuthenticatedUser("1");

        var filtered = SampleExercises.Take(1).ToList();
        _exerciseServiceMock
            .Setup(s => s.GetAllAsync(1, "anx", It.IsAny<CancellationToken>()))
            .ReturnsAsync(filtered);

        var result = await _sut.GetAll("anx", CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_InvalidParameter_Returns400()
    {
        SetupAuthenticatedUser("1");

        var result = await _sut.GetAll("invalid", CancellationToken.None) as BadRequestObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
        _exerciseServiceMock.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_ParameterCaseInsensitive_Returns200()
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.GetAllAsync(1, "ANX", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleExercises.Take(1).ToList());

        var result = await _sut.GetAll("ANX", CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_EmptyList_Returns200WithEmptyArray()
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.GetAllAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExerciseResponse>());

        var result = await _sut.GetAll(null, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        (result.Value as IEnumerable<ExerciseResponse>).Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.GetAll(null, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    // ── GET /api/exercises/{exerciseId} ───────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingExercise_Returns200()
    {
        var exercise = SampleExercises[0];
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exercise);

        var result = await _sut.GetById(1, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(exercise);
    }

    [Fact]
    public async Task GetById_NonExistentExercise_Returns404()
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.GetByIdAsync(1, 999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseResponse?)null);

        var result = await _sut.GetById(999, CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetById_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();

        var result = await _sut.GetById(1, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    // ── PUT /api/exercises/{exerciseId} ───────────────────────────────────────

    [Fact]
    public async Task Update_ExistingExercise_Returns200()
    {
        SetupAuthenticatedUser("1");
        var request = new UpdateExerciseRequest("anx", 8, "8-10");
        var updated = new ExerciseResponse(1, "EX_ANX_01", "anx", 8, "8-10", 10);

        _exerciseServiceMock
            .Setup(s => s.UpdateAsync(1, 1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _sut.Update(1, request, CancellationToken.None) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Update_InvalidParameter_Returns400()
    {
        SetupAuthenticatedUser("1");
        var request = new UpdateExerciseRequest("bad", 8, "8-10");

        var result = await _sut.Update(1, request, CancellationToken.None) as BadRequestObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_UnauthenticatedUser_Returns401()
    {
        SetupAnonymousUser();
        var request = new UpdateExerciseRequest("anx", 8, "8-10");

        var result = await _sut.Update(1, request, CancellationToken.None) as UnauthorizedObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
    }

    // ── DELETE /api/exercises/{exerciseId} ────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingExercise_Returns204()
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.DeleteAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.Delete(1, CancellationToken.None) as NoContentResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_MissingExercise_Returns404()
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.DeleteAsync(1, 999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.Delete(999, CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
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
    public async Task DeleteByJournal_WithExistingExercises_Returns204()
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.DeleteByJournalAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _sut.DeleteByJournal(10, CancellationToken.None) as NoContentResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeleteByJournal_WithNoExercises_Returns404()
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.DeleteByJournalAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _sut.DeleteByJournal(10, CancellationToken.None) as NotFoundObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Theory]
    [InlineData("anx")]
    [InlineData("dep")]
    [InlineData("str")]
    [InlineData("slp")]
    [InlineData("soc")]
    [InlineData("cdt")]
    [InlineData("safe")]
    [InlineData("eng")]
    public async Task GetAll_AllValidParameters_Return200(string param)
    {
        SetupAuthenticatedUser("1");

        _exerciseServiceMock
            .Setup(s => s.GetAllAsync(1, param, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExerciseResponse>());

        var result = await _sut.GetAll(param, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
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
}
