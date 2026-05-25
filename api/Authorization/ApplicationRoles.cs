namespace api.Authorization;

/// <summary>Application role definitions with hierarchy.</summary>
public static class ApplicationRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";

    public static readonly string[] All = [User, Admin, Moderator];

    /// <summary>Check if a role is valid.</summary>
    public static bool IsValid(string role) => All.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>Normalize role name to exact case.</summary>
    public static string? Normalize(string role) => 
        All.FirstOrDefault(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

    /// <summary>Check if a role has admin privileges.</summary>
    public static bool IsAdmin(string role) => role.Equals(Admin, StringComparison.OrdinalIgnoreCase);

    /// <summary>Check if a role has moderator or higher privileges.</summary>
    public static bool IsModerator(string role) => role.Equals(Moderator, StringComparison.OrdinalIgnoreCase) || IsAdmin(role);
}
