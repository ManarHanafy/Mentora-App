namespace api.Authorization;

public static class ApplicationPermissions
{
    // Users Management
    public const string UsersRead = "users:read";
    public const string UsersWrite = "users:write";
    public const string RolesManage = "roles:manage";

    // Journals
    public const string JournalsRead = "journals:read";
    public const string JournalsWrite = "journals:write";
    public const string JournalsDelete = "journals:delete";
    public const string JournalsAnalyze = "journals:analyze";

    // Exercises
    public const string ExercisesRead = "exercises:read";
    public const string ExercisesWrite = "exercises:write";
    public const string ExercisesDelete = "exercises:delete";

    // Chats
    public const string ChatsRead = "chats:read";
    public const string ChatsWrite = "chats:write";
    public const string ChatsDelete = "chats:delete";

    // Moods
    public const string MoodsRead = "moods:read";
    public const string MoodsWrite = "moods:write";
    public const string MoodsDelete = "moods:delete";

    // Statistics
    public const string StatisticsRead = "statistics:read";

    // Account
    public const string AccountRead = "account:read";
    public const string AccountWrite = "account:write";

    // Crisis
    public const string CrisisRead = "crisis:read";

    // AI Operations
    public const string AIAnalyze = "ai:analyze";

    // Onboarding
    public const string OnboardingRead = "onboarding:read";
    public const string OnboardingWrite = "onboarding:write";

    /// <summary>Get all permissions available to a specific role.</summary>
    public static IReadOnlyList<string> GetByRole(string role) => role switch
    {
        ApplicationRoles.Admin => GetAdminPermissions(),
        ApplicationRoles.User => GetUserPermissions(),
        ApplicationRoles.Moderator => GetModeratorPermissions(),
        _ => []
    };

    /// <summary>Admin has all permissions.</summary>
    private static List<string> GetAdminPermissions() =>
    [
        // Users Management
        UsersRead, UsersWrite, RolesManage,

        // Journals
        JournalsRead, JournalsWrite, JournalsDelete, JournalsAnalyze,

        // Exercises
        ExercisesRead, ExercisesWrite, ExercisesDelete,

        // Chats
        ChatsRead, ChatsWrite, ChatsDelete,

        // Moods
        MoodsRead, MoodsWrite, MoodsDelete,

        // Statistics
        StatisticsRead,

        // Account
        AccountRead, AccountWrite,

        // Crisis
        CrisisRead,

        // AI
        AIAnalyze,

        // Onboarding
        OnboardingRead, OnboardingWrite
    ];

    /// <summary>Regular users can read/write their own data and access core features.</summary>
    private static List<string> GetUserPermissions() =>
    [
        JournalsRead, JournalsWrite, JournalsDelete, JournalsAnalyze,
        ExercisesRead, ExercisesWrite, ExercisesDelete,
        ChatsRead, ChatsWrite,
        MoodsRead, MoodsWrite,
        StatisticsRead,
        AccountRead, AccountWrite,
        CrisisRead,
        AIAnalyze,
        OnboardingRead, OnboardingWrite
    ];

    /// <summary>Moderators can read all data and delete inappropriate content.</summary>
    private static List<string> GetModeratorPermissions() =>
    [
        UsersRead,
        JournalsRead, JournalsDelete,
        ExercisesRead,
        ChatsRead, ChatsDelete,
        MoodsRead,
        StatisticsRead,
        CrisisRead,
        OnboardingRead
    ];

    /// <summary>Check if a user has a specific permission based on their role.</summary>
    public static bool HasPermission(string role, string permission)
    {
        var permissions = GetByRole(role);
        return permissions.Contains(permission);
    }

    /// <summary>Get all available permissions.</summary>
    public static IReadOnlyList<string> GetAll() =>
    [
        UsersRead, UsersWrite, RolesManage,
        JournalsRead, JournalsWrite, JournalsDelete, JournalsAnalyze,
        ExercisesRead, ExercisesWrite, ExercisesDelete,
        ChatsRead, ChatsWrite, ChatsDelete,
        MoodsRead, MoodsWrite, MoodsDelete,
        StatisticsRead,
        AccountRead, AccountWrite,
        CrisisRead,
        AIAnalyze,
        OnboardingRead, OnboardingWrite
    ];
}
