using Microsoft.AspNetCore.Authorization;
using api.Extensions;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OnboardingController(IOnboardingService onboardingService, ApplicationDbContext db) : ControllerBase
{
    [HttpGet("questions")]
    [Authorize(Policy = "OnboardingRead")]
    [ProducesResponseType(typeof(OnboardingQuestionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestions([FromQuery] string? locale, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return NotFound(new { error = $"User {userId} not found." });

        var response = await onboardingService.GetQuestionsAsync(userId.Value, locale, cancellationToken);
        return Ok(response);
    }

    [HttpGet("status")]
    [Authorize(Policy = "OnboardingRead")]
    [ProducesResponseType(typeof(OnboardingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return NotFound(new { error = $"User {userId} not found." });

        var response = await onboardingService.GetStatusAsync(userId.Value, cancellationToken);
        return Ok(response);
    }

    [HttpPost("submit")]
    [Authorize(Policy = "OnboardingWrite")]
    [ProducesResponseType(typeof(OnboardingSubmitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit([FromBody] SubmitOnboardingRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return NotFound(new { error = $"User {userId} not found." });

        try
        {
            var response = await onboardingService.SubmitAsync(userId.Value, request, cancellationToken);
            return Ok(response);
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

    [HttpPost("reset/{targetUserId:int}")]
    [Authorize(Policy = "UsersWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reset(int targetUserId, CancellationToken cancellationToken)
    {
        var adminUserId = User.GetUserId();
        if (adminUserId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var reset = await onboardingService.ResetAsync(adminUserId.Value, targetUserId, cancellationToken);
        return reset ? NoContent() : NotFound(new { error = $"User {targetUserId} not found." });
    }

    [HttpPost("reset")]
    [Authorize(Policy = "OnboardingWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetOwn(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var reset = await onboardingService.ResetAsync(userId.Value, userId.Value, cancellationToken);
        return reset ? NoContent() : NotFound(new { error = "User not found." });
    }
}
