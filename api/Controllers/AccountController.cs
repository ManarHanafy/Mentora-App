using Microsoft.AspNetCore.Authorization;
using api.Abstractions;
using api.Extensions;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountController(IAccountService accountService) : ControllerBase
{
    // ── DELETE /api/account ───────────────────────────────────────────────────

    [HttpDelete]
    [Authorize(Policy = "AccountWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized(new { error = "Invalid token claims." });

        var result = await accountService.DeactivateAsync(userId.Value, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
