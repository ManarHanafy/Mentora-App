using Microsoft.AspNetCore.Authorization;
using api.Authorization;

namespace api;

/// <summary>Helper class to register all authorization policies.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Register all authorization policies in the service collection.</summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, ResourceAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            // User Management Policies
            options.AddPolicy("UsersRead", policy => policy.RequireClaim("permission", ApplicationPermissions.UsersRead));
            options.AddPolicy("UsersWrite", policy => policy.RequireClaim("permission", ApplicationPermissions.UsersWrite));
            options.AddPolicy("RolesManage", policy => policy.RequireClaim("permission", ApplicationPermissions.RolesManage));

            // Journal Policies
            options.AddPolicy("JournalsRead", policy => policy.RequireClaim("permission", ApplicationPermissions.JournalsRead));
            options.AddPolicy("JournalsWrite", policy => policy.RequireClaim("permission", ApplicationPermissions.JournalsWrite));
            options.AddPolicy("JournalsDelete", policy => policy.RequireClaim("permission", ApplicationPermissions.JournalsDelete));
            options.AddPolicy("JournalsAnalyze", policy => policy.RequireClaim("permission", ApplicationPermissions.JournalsAnalyze));

            // Exercise Policies
            options.AddPolicy("ExercisesRead", policy => policy.RequireClaim("permission", ApplicationPermissions.ExercisesRead));
            options.AddPolicy("ExercisesWrite", policy => policy.RequireClaim("permission", ApplicationPermissions.ExercisesWrite));
            options.AddPolicy("ExercisesDelete", policy => policy.RequireClaim("permission", ApplicationPermissions.ExercisesDelete));

            // Chat Policies
            options.AddPolicy("ChatsRead", policy => policy.RequireClaim("permission", ApplicationPermissions.ChatsRead));
            options.AddPolicy("ChatsWrite", policy => policy.RequireClaim("permission", ApplicationPermissions.ChatsWrite));
            options.AddPolicy("ChatsDelete", policy => policy.RequireClaim("permission", ApplicationPermissions.ChatsDelete));

            // Mood Policies
            options.AddPolicy("MoodsRead", policy => policy.RequireClaim("permission", ApplicationPermissions.MoodsRead));
            options.AddPolicy("MoodsWrite", policy => policy.RequireClaim("permission", ApplicationPermissions.MoodsWrite));
            options.AddPolicy("MoodsDelete", policy => policy.RequireClaim("permission", ApplicationPermissions.MoodsDelete));

            // Statistics Policy
            options.AddPolicy("StatisticsRead", policy => policy.RequireClaim("permission", ApplicationPermissions.StatisticsRead));

            // Account Policies
            options.AddPolicy("AccountRead", policy => policy.RequireClaim("permission", ApplicationPermissions.AccountRead));
            options.AddPolicy("AccountWrite", policy => policy.RequireClaim("permission", ApplicationPermissions.AccountWrite));

            // Crisis Policy
            options.AddPolicy("CrisisRead", policy => policy.RequireClaim("permission", ApplicationPermissions.CrisisRead));

            // AI Policy
            options.AddPolicy("AIAnalyze", policy => policy.RequireClaim("permission", ApplicationPermissions.AIAnalyze));

            // Onboarding Policies
            options.AddPolicy("OnboardingRead", policy => policy.RequireClaim("permission", ApplicationPermissions.OnboardingRead));
            options.AddPolicy("OnboardingWrite", policy => policy.RequireClaim("permission", ApplicationPermissions.OnboardingWrite));
        });

        return services;
    }
}
