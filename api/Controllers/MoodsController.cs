using Microsoft.AspNetCore.Authorization;
using api.Contracts.Moods;
using api.Extensions;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MoodsController(IMoodService moodService, ApplicationDbContext db) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "MoodsWrite")]
    [ProducesResponseType(typeof(MoodResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] SubmitMoodRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return NotFound(new { error = $"User {userId} not found." });

        try
        {
            var response = await moodService.SubmitAsync(userId.Value, request.Mood, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "MoodsRead")]
    [ProducesResponseType(typeof(MoodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var queryDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await moodService.GetByDateAsync(userId.Value, queryDate, cancellationToken);
        return response is null
            ? NotFound(new { error = $"Mood for {queryDate:yyyy-MM-dd} not found." })
            : Ok(response);
    }

    [HttpGet("history")]
    [Authorize(Policy = "MoodsRead")]
    [ProducesResponseType(typeof(List<MoodResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        if (from is not null && to is not null && from > to)
            return BadRequest(new { error = "from must be less than or equal to to." });

        var response = await moodService.GetHistoryAsync(userId.Value, from, to, cancellationToken);
        return Ok(response);
    }
}
