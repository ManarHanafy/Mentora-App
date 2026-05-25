using Microsoft.EntityFrameworkCore;
using api.Authorization;
using api.Entities;
using api.Persistence.Seeds;
using System.Text.Json;

namespace api.Persistence;

/// <summary>
/// Called once at startup — applies pending EF Core migrations.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        await SeedAdminUserAsync(db, configuration, logger);
        await SeedOnboardingQuestionsAsync(db, logger);
        logger.LogInformation("✓ Database ready.");
    }

    /// <summary>
    /// ONLY call this method if you want to reset the database to a clean state.
    /// This will DELETE all existing data. Use with caution!
    /// </summary>
    public static async Task ResetDatabaseAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            logger.LogWarning("⚠ DROPPING DATABASE - ALL DATA WILL BE LOST");

            await db.Database.EnsureDeletedAsync();
            logger.LogInformation("Database dropped.");

            logger.LogInformation("Applying migrations...");
            await db.Database.MigrateAsync();

            logger.LogInformation("✓ Database reset complete - fresh start.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting database");
            throw;
        }
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext db, IConfiguration configuration, ILogger logger)
    {
        var adminEmail = configuration["AdminSeed:Email"];
        var adminPassword = configuration["AdminSeed:Password"];
        var firstName = configuration["AdminSeed:FirstName"] ?? "System";
        var lastName = configuration["AdminSeed:LastName"] ?? "Admin";
        var username = configuration["AdminSeed:Username"] ?? "admin";

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword) ||
            adminEmail.Equals("SET_IN_ENVIRONMENT", StringComparison.OrdinalIgnoreCase) ||
            adminPassword.Equals("SET_IN_ENVIRONMENT", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Admin seed skipped (AdminSeed credentials are not configured).");
            return;
        }

        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (existing is not null)
        {
            if (!string.Equals(existing.Role, ApplicationRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                existing.Role = ApplicationRoles.Admin;
                await db.SaveChangesAsync();
            }
            logger.LogInformation("Admin seed verified.");
            return;
        }

        var adminUser = new User
        {
            Email = normalizedEmail,
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = ApplicationRoles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordChangedAt = DateTime.UtcNow,
            ParameterSnapshot = new UserParameterSnapshot
            {
                UpdatedAt = DateTime.UtcNow
            }
        };

        db.Users.Add(adminUser);
        await db.SaveChangesAsync();
        logger.LogInformation("Admin user seeded successfully.");
    }

    private static async Task SeedOnboardingQuestionsAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.OnboardingQuestions.AnyAsync())
        {
            logger.LogInformation("Onboarding question seed skipped (already populated).");
            return;
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var questions = new List<OnboardingQuestion>();

        foreach (var seed in OnboardingSeedData.Questions)
        {
            var question = new OnboardingQuestion
            {
                QuestionId = seed.QuestionId,
                Locale = OnboardingSeedData.DefaultLocale,
                Category = seed.Category,
                Parameter = seed.Parameter,
                QuestionText = seed.QuestionText,
                InputControlType = seed.InputControlType,
                ScoringNote = seed.ScoringNote,
                MaxAllowedSelections = seed.MaxAllowedSelections,
                IsSensitiveQuestion = seed.IsSensitiveQuestion,
                PreQuestionDisclaimer = seed.PreQuestionDisclaimer,
                ConditionalActionsJson = seed.ConditionalActions is null
                    ? null
                    : JsonSerializer.Serialize(seed.ConditionalActions, options),
                DisplayOrder = seed.QuestionId,
                IsActive = true
            };

            foreach (var optionSeed in seed.ResponseOptions)
            {
                var option = new OnboardingQuestionOption
                {
                    OptionId = optionSeed.OptionId,
                    OptionText = optionSeed.OptionText,
                    ScorePoints = optionSeed.ScorePoints,
                    DisplayOrder = optionSeed.OptionId,
                    IsActive = true
                };

                if (optionSeed.MetricModifiers is not null)
                {
                    foreach (var modifier in optionSeed.MetricModifiers)
                    {
                        var metricModifier = new OnboardingOptionMetricModifier
                        {
                            Parameter = modifier.Key,
                            ModifierValue = int.TryParse(modifier.Value, out var numeric) ? numeric : null,
                            ModifierValueText = int.TryParse(modifier.Value, out _) ? null : modifier.Value
                        };
                        option.MetricModifiers.Add(metricModifier);
                    }
                }

                question.Options.Add(option);
            }

            questions.Add(question);
        }

        db.OnboardingQuestions.AddRange(questions);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} onboarding questions.", questions.Count);
    }
}
