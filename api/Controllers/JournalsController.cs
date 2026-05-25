using Microsoft.AspNetCore.Authorization;
using api.Contracts.Common;
using api.Extensions;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class JournalsController(IJournalService journalService, ApplicationDbContext db) : ControllerBase
{
    // ── POST /api/journals ─────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "JournalsWrite")]
    [ProducesResponseType(typeof(JournalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create([FromBody] SubmitJournalRequest request, CancellationToken cancellationToken)
    {
        // Always derive userId from the JWT token; the request body must NOT contain a user ID
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        if (!User.IsActive())
            return Forbid("Your account is inactive.");

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return NotFound(new { error = $"User {userId} not found." });

        try
        {
            var result = await journalService.SubmitAsync(userId.Value, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    // ── GET /api/journals/{journalId} ────────────────────────────────────────

    [HttpGet("{journalId:int}")]
    [Authorize(Policy = "JournalsRead")]
    [ProducesResponseType(typeof(JournalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int journalId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var journal = await db.JournalEntries.AsNoTracking().FirstOrDefaultAsync(j => j.Id == journalId, cancellationToken);
        if (journal is null)
            return NotFound(new { error = $"Journal entry {journalId} not found." });

        // Users can only read their own journals unless they're admin
        if (journal.UserId != userId.Value && !User.IsAdmin())
            return Forbid();

        var result = await journalService.GetByIdAsync(journalId, cancellationToken);
        if (result is null)
            return NotFound(new { error = $"Journal entry {journalId} not found." });

        return Ok(result);
    }

    // ── GET /api/journals ─────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = "JournalsRead")]
    [ProducesResponseType(typeof(PagedResponse<JournalSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        if (pageNumber < 1)
            return BadRequest(new { error = "pageNumber must be at least 1." });
        if (pageSize is < 1 or > 100)
            return BadRequest(new { error = "pageSize must be between 1 and 100." });

        var history = await journalService.GetHistoryAsync(userId.Value, pageNumber, pageSize, cancellationToken);
        return Ok(history);
    }

    // ── GET /api/journals/trend ───────────────────────────────────────────────

    [HttpGet("trend")]
    [Authorize(Policy = "JournalsRead")]
    [ProducesResponseType(typeof(List<ParameterTrendResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTrend([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 50)
            return BadRequest(new { error = "Limit must be between 1 and 50." });

        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var trend = await db.JournalEntries
            .Where(e => e.UserId == userId)
            .Include(e => e.Score)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var response = trend.Where(e => e.Score is not null)
            .OrderBy(e => e.CreatedAt)
            .SelectMany(e => new[]
            {
                new { Parameter = "anx", Timestamp = e.CreatedAt, Value = e.Score!.Anx },
                new { Parameter = "dep", Timestamp = e.CreatedAt, Value = e.Score!.Dep },
                new { Parameter = "str", Timestamp = e.CreatedAt, Value = e.Score!.Str },
                new { Parameter = "slp", Timestamp = e.CreatedAt, Value = e.Score!.Slp },
                new { Parameter = "soc", Timestamp = e.CreatedAt, Value = e.Score!.Soc },
                new { Parameter = "cdt", Timestamp = e.CreatedAt, Value = e.Score!.Cdt },
                new { Parameter = "safe", Timestamp = e.CreatedAt, Value = e.Score!.Safe },
                new { Parameter = "eng", Timestamp = e.CreatedAt, Value = e.Score!.Eng }
            })
            .GroupBy(x => x.Parameter)
            .Select(g => new ParameterTrendResponse(
                g.Key,
                g.OrderBy(p => p.Timestamp)
                 .Select(p => new TrendPoint(p.Timestamp, p.Value))
                 .ToList()))
            .OrderBy(r => r.Parameter)
            .ToList();

        return Ok(response);
    }

    // ── GET /api/journals/parameters ─────────────────────────────────────────

    [HttpGet("parameters")]
    [Authorize(Policy = "JournalsRead")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserParameterSnapshots([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "Limit must be between 1 and 100." });

        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var entries = await db.JournalEntries
            .Where(e => e.UserId == userId)
            .Include(e => e.Score)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var snapshots = entries
            .OrderBy(e => e.CreatedAt)
            .Select(e => new
            {
                journalEntryId = e.Id,
                date           = e.CreatedAt,
                riskLevel      = e.RiskLevel,
                parameters     = e.Score is null
                    ? new Dictionary<string, int>()
                    : new Dictionary<string, int>
                    {
                        ["anx"] = e.Score.Anx,
                        ["dep"] = e.Score.Dep,
                        ["str"] = e.Score.Str,
                        ["slp"] = e.Score.Slp,
                        ["soc"] = e.Score.Soc,
                        ["cdt"] = e.Score.Cdt,
                        ["safe"] = e.Score.Safe,
                        ["eng"] = e.Score.Eng
                    }
            })
            .ToList();

        return Ok(snapshots);
    }

    // ── PUT /api/journals/{journalId} ─────────────────────────────────────────

    [HttpPut("{journalId:int}")]
    [Authorize(Policy = "JournalsWrite")]
    [ProducesResponseType(typeof(JournalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Update(int journalId, [FromBody] UpdateJournalRequest request, CancellationToken cancellationToken)
    {
        // Ownership: authenticated user must own the journal
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var journal = await db.JournalEntries.FirstOrDefaultAsync(j => j.Id == journalId, cancellationToken);
        if (journal is null)
            return NotFound(new { error = $"Journal entry {journalId} not found." });

        if (journal.UserId != userId.Value && !User.IsAdmin())
            return Forbid();

        try
        {
            var result = await journalService.UpdateAsync(journalId, request, cancellationToken);
            return result is null ? NotFound(new { error = $"Journal entry {journalId} not found." }) : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    // ── DELETE /api/journals/{journalId} ──────────────────────────────────────

    [HttpDelete("{journalId:int}")]
    [Authorize(Policy = "JournalsDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int journalId, CancellationToken cancellationToken)
    {
        // Ownership: authenticated user must own the journal unless they're admin
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var journal = await db.JournalEntries.FirstOrDefaultAsync(j => j.Id == journalId, cancellationToken);
        if (journal is null)
            return NotFound(new { error = $"Journal entry {journalId} not found." });

        if (journal.UserId != userId.Value && !User.IsAdmin())
            return Forbid();

        var deleted = await journalService.DeleteAsync(journalId, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = $"Journal entry {journalId} not found." });
    }
}
