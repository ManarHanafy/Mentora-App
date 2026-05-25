using System.Text.Json;
using api.Contracts.Onboarding;
using api.Entities;
using api.Infrastructure.Caching;
using api.Persistence;
using api.Persistence.Seeds;
using api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace api.Tests.Services;

public class OnboardingServiceTests
{
    [Fact]
    public async Task SubmitAsync_ValidAnswers_PersistsStateAndUpdatesSnapshot()
    {
        await using var db = CreateDbContext();
        SeedUser(db, 1);
        SeedQuestions(db);
        var service = CreateService(db);

        var request = BuildRequest();

        var response = await service.SubmitAsync(1, request, CancellationToken.None);

        response.Completed.Should().BeTrue();
        response.Parameters.Anx.Should().Be(0);
        response.Parameters.Dep.Should().Be(0);
        response.Parameters.Str.Should().Be(2);
        response.Parameters.Slp.Should().Be(7);
        response.Parameters.Soc.Should().Be(5);
        response.Parameters.Cdt.Should().Be(0);
        response.Parameters.Safe.Should().Be(0);
        response.Parameters.Eng.Should().Be(6);

        var state = await db.UserOnboardingStates.SingleAsync();
        state.IsCompleted.Should().BeTrue();
        state.CompletedAt.Should().NotBeNull();

        (await db.UserOnboardingResponses.CountAsync()).Should().Be(10);
        (await db.UserOnboardingResponseOptions.CountAsync()).Should().Be(11);
        (await db.UserOnboardingResults.CountAsync()).Should().Be(1);

        var snapshot = await db.UserParameterSnapshots.SingleAsync(s => s.UserId == 1);
        snapshot.Str.Should().Be(2);
        snapshot.Slp.Should().Be(7);
        snapshot.Soc.Should().Be(5);
        snapshot.Eng.Should().Be(6);
    }

    [Fact]
    public async Task SubmitAsync_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        await using var db = CreateDbContext();
        SeedUser(db, 2);
        SeedQuestions(db);
        var service = CreateService(db);
        var request = BuildRequest();

        await service.SubmitAsync(2, request, CancellationToken.None);

        Func<Task> act = () => service.SubmitAsync(2, request, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IOnboardingService CreateService(ApplicationDbContext db)
    {
        var cache = new AppCacheService(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
        return new OnboardingService(db, cache, new OnboardingScoringEngine(), NullLogger<OnboardingService>.Instance);
    }

    private static void SeedUser(ApplicationDbContext db, int userId)
    {
        db.Users.Add(new User
        {
            Id = userId,
            Username = $"user{userId}",
            Email = $"user{userId}@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            ParameterSnapshot = new UserParameterSnapshot
            {
                UpdatedAt = DateTime.UtcNow
            }
        });
        db.SaveChanges();
    }

    private static void SeedQuestions(ApplicationDbContext db)
    {
        if (db.OnboardingQuestions.Any())
            return;

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
                        option.MetricModifiers.Add(new OnboardingOptionMetricModifier
                        {
                            Parameter = modifier.Key,
                            ModifierValue = int.TryParse(modifier.Value, out var numeric) ? numeric : null,
                            ModifierValueText = int.TryParse(modifier.Value, out _) ? null : modifier.Value
                        });
                    }
                }

                question.Options.Add(option);
            }

            questions.Add(question);
        }

        db.OnboardingQuestions.AddRange(questions);
        db.SaveChanges();
    }

    private static SubmitOnboardingRequest BuildRequest()
    {
        var answers = OnboardingSeedData.Questions
            .Select(q => new OnboardingAnswerRequest(
                q.QuestionId,
                q.QuestionId == 9 ? [1, 2] : [1]))
            .ToList();

        return new SubmitOnboardingRequest(answers);
    }
}
