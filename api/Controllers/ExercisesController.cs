using Microsoft.AspNetCore.Authorization;
using api.Extensions;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExercisesController(IExerciseService exerciseService) : ControllerBase
{
    private static readonly string[] _validParameters = ["anx", "dep", "str", "slp", "soc", "cdt", "safe", "eng"];

    [HttpGet]
    [Authorize(Policy = "ExercisesRead")]
    [ProducesResponseType(typeof(List<ExerciseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] string? parameter, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        if (!string.IsNullOrWhiteSpace(parameter) && !_validParameters.Contains(parameter.Trim().ToLowerInvariant()))
            return BadRequest(new { error = $"Invalid parameter '{parameter}'. Valid codes: {string.Join(", ", _validParameters)}" });

        var exercises = await exerciseService.GetAllAsync(userId.Value, parameter, cancellationToken);
        return Ok(exercises);
    }

    [HttpGet("{exerciseId:int}")]
    [Authorize(Policy = "ExercisesRead")]
    [ProducesResponseType(typeof(ExerciseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int exerciseId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var exercise = await exerciseService.GetByIdAsync(userId.Value, exerciseId, cancellationToken);
        return exercise is null ? NotFound(new { error = $"Exercise {exerciseId} not found." }) : Ok(exercise);
    }

    [HttpPut("{exerciseId:int}")]
    [Authorize(Policy = "ExercisesWrite")]
    [ProducesResponseType(typeof(ExerciseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int exerciseId, [FromBody] UpdateExerciseRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        if (string.IsNullOrWhiteSpace(request.Parameter) || !_validParameters.Contains(request.Parameter.Trim().ToLowerInvariant()))
            return BadRequest(new { error = $"Invalid parameter '{request.Parameter}'. Valid codes: {string.Join(", ", _validParameters)}" });

        var updated = await exerciseService.UpdateAsync(userId.Value, exerciseId, request, cancellationToken);
        return updated is null ? NotFound(new { error = $"Exercise {exerciseId} not found." }) : Ok(updated);
    }

    [HttpDelete("{exerciseId:int}")]
    [Authorize(Policy = "ExercisesDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int exerciseId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var deleted = await exerciseService.DeleteAsync(userId.Value, exerciseId, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = $"Exercise {exerciseId} not found." });
    }

    [HttpDelete("journal/{journalId:int}")]
    [Authorize(Policy = "ExercisesDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteByJournal(int journalId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var deletedCount = await exerciseService.DeleteByJournalAsync(userId.Value, journalId, cancellationToken);
        return deletedCount == 0
            ? NotFound(new { error = $"No exercises found for journal {journalId}." })
            : NoContent();
    }
}
