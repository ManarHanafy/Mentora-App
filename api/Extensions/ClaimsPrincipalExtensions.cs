using System.Security.Claims;
using api.Authorization;

namespace api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the integer user ID from the "sub" claim.
    /// Returns null if the claim is missing or cannot be parsed.
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("sub")?.Value
                 ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(value, out var id) ? id : null;
    }

    /// <summary>Gets the user's role from claims.</summary>
    public static string? GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>Gets the user's email from claims.</summary>
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
    }

    /// <summary>Gets whether the user is active from the isActive claim.</summary>
    public static bool IsActive(this ClaimsPrincipal user)
    {
        var isActiveClaim = user.FindFirst("isActive")?.Value;
        return bool.TryParse(isActiveClaim, out var isActive) && isActive;
    }

    /// <summary>Gets whether the user's email is verified from the emailVerified claim.</summary>
    public static bool IsEmailVerified(this ClaimsPrincipal user)
    {
        var emailVerifiedClaim = user.FindFirst("emailVerified")?.Value;
        return bool.TryParse(emailVerifiedClaim, out var verified) && verified;
    }

    /// <summary>Check if user has a specific permission.</summary>
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        return user.FindAll("permission").Any(c => c.Value == permission);
    }

    /// <summary>Check if user has any of the specified permissions.</summary>
    public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
    {
        var userPermissions = user.FindAll("permission").Select(c => c.Value).ToList();
        return permissions.Any(p => userPermissions.Contains(p));
    }

    /// <summary>Check if user has all of the specified permissions.</summary>
    public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
    {
        var userPermissions = user.FindAll("permission").Select(c => c.Value).ToList();
        return permissions.All(p => userPermissions.Contains(p));
    }

    /// <summary>Check if user is an admin.</summary>
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        var role = user.GetRole();
        return role != null && ApplicationRoles.IsAdmin(role);
    }

    /// <summary>Check if user is a moderator or higher.</summary>
    public static bool IsModerator(this ClaimsPrincipal user)
    {
        var role = user.GetRole();
        return role != null && ApplicationRoles.IsModerator(role);
    }

    /// <summary>Get all permissions for the user.</summary>
    public static List<string> GetPermissions(this ClaimsPrincipal user)
    {
        return user.FindAll("permission").Select(c => c.Value).ToList();
    }
}
