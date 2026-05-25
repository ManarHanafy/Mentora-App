using Microsoft.AspNetCore.Authorization;
using api.Abstractions;
using api.Extensions;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    // ── GET /api/statistics/me ────────────────────────────────────────────────

    [HttpGet("me")]
    [Authorize(Policy = "StatisticsRead")]
    [ProducesResponseType(typeof(UserStatisticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyStatistics(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized(new { error = "Invalid token claims." });

        var result = await statisticsService.GetUserStatisticsAsync(userId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
