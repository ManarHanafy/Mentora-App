using Microsoft.AspNetCore.Authorization;
using api.Entities;
using api.Persistence;

namespace api.Authorization;

/// <summary>
/// Custom authorization handler for resource-level access control.
/// Ensures users can only access their own data unless they are admins or have specific permissions.
/// </summary>
public class ResourceAuthorizationHandler(ApplicationDbContext db, ILogger<ResourceAuthorizationHandler> logger) : AuthorizationHandler<ResourceAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAuthorizationRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst("sub")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            context.Fail();
            return;
        }

        // Admins bypass resource-level checks
        var rolesClaim = context.User.FindFirst("role")?.Value;
        if (rolesClaim != null && ApplicationRoles.IsAdmin(rolesClaim))
        {
            context.Succeed(requirement);
            return;
        }

        // Check if user has the required permission
        var permissionClaim = context.User.FindAll("permission")
            .FirstOrDefault(c => c.Value == requirement.Permission);

        if (permissionClaim == null)
        {
            logger.LogWarning("User {UserId} does not have permission {Permission}", userId, requirement.Permission);
            context.Fail();
            return;
        }

        // If resource type is specified, verify ownership
        if (!string.IsNullOrEmpty(requirement.ResourceType))
        {
            var resourceId = requirement.ResourceId;
            var hasAccess = await VerifyResourceOwnershipAsync(userId, requirement.ResourceType, resourceId);

            if (!hasAccess)
            {
                logger.LogWarning("User {UserId} attempted unauthorized access to {ResourceType} {ResourceId}", userId, requirement.ResourceType, resourceId);
                context.Fail();
                return;
            }
        }

        context.Succeed(requirement);
    }

    /// <summary>Verify if a user owns or can access a specific resource.</summary>
    private async Task<bool> VerifyResourceOwnershipAsync(int userId, string resourceType, int? resourceId)
    {
        if (!resourceId.HasValue)
            return false;

        return resourceType.ToLowerInvariant() switch
        {
            "journal" => await db.JournalEntries.AnyAsync(j => j.Id == resourceId.Value && j.UserId == userId),
            "exercise" => await db.SuggestedExercises
                .Include(e => e.JournalEntry)
                .AnyAsync(e => e.Id == resourceId.Value && e.JournalEntry != null && e.JournalEntry.UserId == userId),
            "chat" => await db.Chats.AnyAsync(c => c.Id == resourceId.Value && c.UserId == userId),
            "mood" => await db.MoodEntries.AnyAsync(m => m.Id == resourceId.Value && m.UserId == userId),
            "user" => resourceId.Value == userId, // Users can only access their own profile
            _ => false
        };
    }
}

/// <summary>Authorization requirement for resource-level access control.</summary>
public class ResourceAuthorizationRequirement(string permission, string? resourceType = null, int? resourceId = null) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
    public string? ResourceType { get; } = resourceType;
    public int? ResourceId { get; } = resourceId;
}
