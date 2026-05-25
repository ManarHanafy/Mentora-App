using Microsoft.AspNetCore.Authorization;
using api.Contracts.Crisis;
using api.Extensions;

namespace api.Controllers;

/// <summary>
/// Crisis resources controller — returns emergency contacts and support guidance
/// when a user's journal analysis returns risk_level = "crisis".
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CrisisController : ControllerBase
{
    private readonly ICrisisResourceService _resourceService;

    public CrisisController(ICrisisResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    // ── GET /api/crisis/resources ─────────────────────────────────────────────

    [HttpGet("resources")]
    [Authorize(Policy = "CrisisRead")]
    [ProducesResponseType(typeof(CrisisResourcesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetResources([FromQuery] string? locale = null, [FromQuery] string? country = null)
    {
        return Ok(_resourceService.GetResources(locale, country));
    }

    // ── GET /api/crisis/check/{userId} ────────────────────────────────────────
    // Returns crisis resources for a specific user together with their latest risk level.

    [HttpGet("check/{userId:int}")]
    [Authorize(Policy = "CrisisRead")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckUserCrisisStatus(
        int userId,
        [FromServices] ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var authenticatedUserId = User.GetUserId();
        if (authenticatedUserId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        if (authenticatedUserId.Value != userId && !User.IsAdmin())
            return Forbid();

        var latest = await db.JournalEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.RiskLevel, e.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
            return NotFound(new { error = $"No journal entries found for user {userId}." });

        return Ok(new
        {
            userId,
            latestRiskLevel = latest.RiskLevel,
            isCrisis        = latest.RiskLevel == "crisis",
            checkedAt       = DateTime.UtcNow,
            resources       = latest.RiskLevel == "crisis"
                ? _resourceService.GetResources(null, null)
                : null
        });
    }
}
