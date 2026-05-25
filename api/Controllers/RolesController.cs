using Microsoft.AspNetCore.Authorization;
using api.Authorization;
using api.Contracts.Users;
using api.Extensions;
using api.Infrastructure.Audit;
using api.Persistence;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "RolesManage")]
public class RolesController(
    IUserService userService,
    ApplicationDbContext db,
    IAuditLogger auditLogger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetRoles()
    {
        var roles = ApplicationRoles.All
            .Select(role => new RoleResponse(role, ApplicationPermissions.GetByRole(role)))
            .ToList();

        return Ok(roles);
    }

    [HttpPut("users/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var oldRole = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(cancellationToken);

        var result = await userService.UpdateRoleAsync(userId, request.Role, cancellationToken);
        if (!result.IsSuccess)
            return result.ToProblem();

        var adminId = User.GetUserId();
        if (adminId is not null && oldRole is not null)
            await auditLogger.LogPermissionChangeAsync(adminId.Value, userId, oldRole, request.Role);

        return NoContent();
    }
}
