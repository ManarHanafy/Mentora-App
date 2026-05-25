using System.Text.Json;
using api.Contracts.Onboarding;
using api.Contracts.Users;
using api.Infrastructure.Caching;
using api.Persistence.Seeds;

namespace api.Services;

public class OnboardingService(
    ApplicationDbContext db,
    IAppCacheService cache,
    IOnboardingScoringEngine scoringEngine,
    ILogger<OnboardingService> logger) : IOnboardingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OnboardingQuestionsResponse> GetQuestionsAsync(int userId, string? locale, CancellationToken cancellationToken = default)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var status = await GetStatusInternalAsync(userId, cancellationToken);
        var questions = await cache.GetOrCreateAsync(
            GetQuestionsCacheKey(normalizedLocale),
            async () => await LoadQuestionResponsesAsync(normalizedLocale, cancellationToken),
            TimeSpan.FromMinutes(10),
            cancellationToken);

        return new OnboardingQuestionsResponse(
            status.Completed,
            status.CompletedAt,
            !status.Completed,
            normalizedLocale,
            questions);
    }

    public async Task<OnboardingStatusResponse> GetStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var status = await GetStatusInternalAsync(userId, cancellationToken);
        return new OnboardingStatusResponse(status.Completed, status.CompletedAt, !status.Completed, status.Parameters);
    }

    public async Task<OnboardingSubmitResponse> SubmitAsync(int userId, SubmitOnboardingRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedLocale = NormalizeLocale(request.Locale);
        var questions = await LoadQuestionsAsync(normalizedLocale, cancellationToken);
        if (questions.Count == 0)
            throw new InvalidOperationException("Onboarding questions are not configured.");
        var mappedAnswers = ValidateAndMapAnswers(request, questions);
        var scoreResult = scoringEngine.ComputeScores(mappedAnswers.ScoringAnswers);
        var actions = BuildActions(mappedAnswers.ActionCodes);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var state = await db.UserOnboardingStates
                .Include(s => s.Responses)
                    .ThenInclude(r => r.SelectedOptions)
                .Include(s => s.Result)
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

            if (state?.IsCompleted == true)
                throw new InvalidOperationException("Onboarding has already been completed.");

            if (state is null)
            {
                state = new UserOnboardingState
                {
                    UserId = userId
                };
                db.UserOnboardingStates.Add(state);
            }

            if (state.Responses.Count > 0)
            {
                db.UserOnboardingResponseOptions.RemoveRange(state.Responses.SelectMany(r => r.SelectedOptions));
                db.UserOnboardingResponses.RemoveRange(state.Responses);
            }

            if (state.Result is not null)
                db.UserOnboardingResults.Remove(state.Result);

            var now = DateTime.UtcNow;
            state.IsCompleted = true;
            state.CompletedAt = now;
            state.RawResponsesJson = JsonSerializer.Serialize(request, JsonOptions);

            foreach (var answer in mappedAnswers.Answers)
            {
                var response = new UserOnboardingResponse
                {
                    State = state,
                    UserId = userId,
                    OnboardingQuestionId = answer.Question.Id,
                    QuestionId = answer.Question.QuestionId,
                    LocaleSnapshot = answer.Question.Locale,
                    CategorySnapshot = answer.Question.Category,
                    ParameterSnapshot = answer.Question.Parameter,
                    QuestionTextSnapshot = answer.Question.QuestionText,
                    InputControlTypeSnapshot = answer.Question.InputControlType,
                    ScoringNoteSnapshot = answer.Question.ScoringNote,
                    MaxAllowedSelectionsSnapshot = answer.Question.MaxAllowedSelections,
                    IsSensitiveQuestionSnapshot = answer.Question.IsSensitiveQuestion,
                    PreQuestionDisclaimerSnapshot = answer.Question.PreQuestionDisclaimer,
                    ConditionalActionsSnapshotJson = answer.Question.ConditionalActionsJson
                };

                foreach (var option in answer.SelectedOptions)
                {
                    response.SelectedOptions.Add(new UserOnboardingResponseOption
                    {
                        OnboardingQuestionOptionId = option.Id,
                        OptionId = option.OptionId,
                        OptionTextSnapshot = option.OptionText,
                        ScorePointsSnapshot = option.ScorePoints,
                        MetricModifiersSnapshotJson = option.MetricModifiers.Count == 0
                            ? null
                            : JsonSerializer.Serialize(
                                option.MetricModifiers.ToDictionary(
                                    m => m.Parameter,
                                    m => m.ModifierValue is not null
                                        ? (object)m.ModifierValue.Value
                                        : m.ModifierValueText ?? string.Empty),
                                JsonOptions)
                    });
                }

                state.Responses.Add(response);
            }

            var result = new UserOnboardingResult
            {
                State = state,
                UserId = userId,
                CompletedAt = now,
                Anx = scoreResult.Parameters.Anx,
                Dep = scoreResult.Parameters.Dep,
                Str = scoreResult.Parameters.Str,
                Slp = scoreResult.Parameters.Slp,
                Soc = scoreResult.Parameters.Soc,
                Cdt = scoreResult.Parameters.Cdt,
                Safe = scoreResult.Parameters.Safe,
                Eng = scoreResult.Parameters.Eng
            };
            state.Result = result;

            var snapshot = await LoadOrCreateUserSnapshotAsync(userId, cancellationToken);
            var existingScores = snapshot.ToParametersDictionary();
            var newScores = new Dictionary<string, int>
            {
                ["anx"] = Math.Clamp(existingScores.GetValueOrDefault("anx", 0) + scoreResult.Parameters.Anx, 0, 20),
                ["dep"] = Math.Clamp(existingScores.GetValueOrDefault("dep", 0) + scoreResult.Parameters.Dep, 0, 20),
                ["str"] = Math.Clamp(existingScores.GetValueOrDefault("str", 0) + scoreResult.Parameters.Str, 0, 20),
                ["slp"] = Math.Clamp(existingScores.GetValueOrDefault("slp", 0) + scoreResult.Parameters.Slp, 0, 20),
                ["soc"] = Math.Clamp(existingScores.GetValueOrDefault("soc", 0) + scoreResult.Parameters.Soc, 0, 20),
                ["cdt"] = Math.Clamp(existingScores.GetValueOrDefault("cdt", 0) + scoreResult.Parameters.Cdt, 0, 20),
                ["safe"] = Math.Clamp(existingScores.GetValueOrDefault("safe", 0) + scoreResult.Parameters.Safe, 0, 20),
                ["eng"] = Math.Clamp(existingScores.GetValueOrDefault("eng", 0) + scoreResult.Parameters.Eng, 0, 20)
            };
            snapshot.UpdateFromDictionary(newScores);

            await db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            InvalidateCaches(userId, normalizedLocale);

            return new OnboardingSubmitResponse(
                Success: true,
                Completed: true,
                CompletedAt: now,
                Parameters: scoreResult.Parameters,
                Actions: actions);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Onboarding submission failed due to database constraint. UserId={UserId}", userId);
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("Onboarding has already been completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Onboarding submission failed. UserId={UserId}", userId);
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> ResetAsync(int adminUserId, int targetUserId, CancellationToken cancellationToken = default)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == targetUserId, cancellationToken);
        if (!userExists)
            return false;

        var state = await db.UserOnboardingStates
            .Include(s => s.Responses)
                .ThenInclude(r => r.SelectedOptions)
            .Include(s => s.Result)
            .FirstOrDefaultAsync(s => s.UserId == targetUserId, cancellationToken);

        if (state is null)
            return true;

        // Only clear responses and result to allow re-assessment, don't touch scores
        if (state.Responses.Count > 0)
        {
            db.UserOnboardingResponseOptions.RemoveRange(state.Responses.SelectMany(r => r.SelectedOptions));
            db.UserOnboardingResponses.RemoveRange(state.Responses);
        }

        if (state.Result is not null)
            db.UserOnboardingResults.Remove(state.Result);

        state.IsCompleted = false;
        state.CompletedAt = null;
        state.RawResponsesJson = null;

        await db.SaveChangesAsync(cancellationToken);

        InvalidateCaches(targetUserId, OnboardingSeedData.DefaultLocale);
        logger.LogInformation("Admin {AdminId} reset onboarding for user {UserId}", adminUserId, targetUserId);
        return true;
    }

    private static string NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return OnboardingSeedData.DefaultLocale;

        return locale.Trim().ToLowerInvariant();
    }

    private async Task<List<OnboardingQuestion>> LoadQuestionsAsync(string locale, CancellationToken cancellationToken)
    {
        return await db.OnboardingQuestions
            .AsNoTracking()
            .Where(q => q.IsActive && q.Locale == locale)
            .Include(q => q.Options.Where(o => o.IsActive))
                .ThenInclude(o => o.MetricModifiers)
            .OrderBy(q => q.DisplayOrder)
            .ThenBy(q => q.QuestionId)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<OnboardingQuestionResponse>> LoadQuestionResponsesAsync(string locale, CancellationToken cancellationToken)
    {
        var questions = await LoadQuestionsAsync(locale, cancellationToken);
        return questions
            .Select(question => new OnboardingQuestionResponse(
                QuestionId: question.QuestionId,
                Category: question.Category,
                Parameter: question.Parameter,
                QuestionText: question.QuestionText,
                InputControlType: question.InputControlType,
                ResponseOptions: question.Options
                    .OrderBy(o => o.DisplayOrder)
                    .ThenBy(o => o.OptionId)
                    .Select(option => new OnboardingOptionResponse(
                        OptionId: option.OptionId,
                        OptionText: option.OptionText,
                        ScorePoints: option.ScorePoints,
                        MetricModifiers: option.MetricModifiers.Count == 0
                            ? null
                            : option.MetricModifiers.ToDictionary(
                                m => m.Parameter,
                                m => m.ModifierValue is not null
                                    ? (object)m.ModifierValue.Value
                                    : m.ModifierValueText ?? string.Empty)))
                    .ToList(),
                ScoringNote: question.ScoringNote,
                MaxAllowedSelections: question.MaxAllowedSelections,
                IsSensitiveQuestion: question.IsSensitiveQuestion,
                PreQuestionDisclaimer: question.PreQuestionDisclaimer,
                ConditionalActions: BuildConditionalActionMap(question.ConditionalActionsJson)))
            .ToList();
    }

    private static Dictionary<int, OnboardingActionMetadata>? BuildConditionalActionMap(string? conditionalActionsJson)
    {
        if (string.IsNullOrWhiteSpace(conditionalActionsJson))
            return null;

        var parsed = JsonSerializer.Deserialize<Dictionary<int, string>>(conditionalActionsJson, JsonOptions)
            ?? new Dictionary<int, string>();

        var mapped = new Dictionary<int, OnboardingActionMetadata>();
        foreach (var (optionId, code) in parsed)
        {
            var metadata = MapAction(code);
            if (metadata is not null)
                mapped[optionId] = metadata;
        }

        return mapped.Count == 0 ? null : mapped;
    }

    private static List<OnboardingActionMetadata> BuildActions(List<string> actionCodes)
    {
        var results = new List<OnboardingActionMetadata>();
        foreach (var code in actionCodes.Distinct())
        {
            var metadata = MapAction(code);
            if (metadata is not null)
                results.Add(metadata);
        }

        return results;
    }

    private static OnboardingActionMetadata? MapAction(string actionCode)
    {
        if (string.Equals(actionCode, "continue_normally", StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.Equals(actionCode, "flag_elevated_monitoring_show_supportive_message", StringComparison.OrdinalIgnoreCase))
        {
            return new OnboardingActionMetadata(
                Code: actionCode,
                Type: "show_supportive_message",
                Severity: "moderate",
                Flags: ["elevated_monitoring"]);
        }

        if (string.Equals(actionCode, "immediately_show_crisis_resources_before_continuing", StringComparison.OrdinalIgnoreCase))
        {
            return new OnboardingActionMetadata(
                Code: actionCode,
                Type: "show_crisis_resources",
                Severity: "high",
                Flags: ["immediate"]);
        }

        return new OnboardingActionMetadata(
            Code: actionCode,
            Type: "notify",
            Severity: "info");
    }

    private (List<ValidatedAnswer> Answers, List<OnboardingScoringAnswer> ScoringAnswers, List<string> ActionCodes) ValidateAndMapAnswers(
        SubmitOnboardingRequest request,
        List<OnboardingQuestion> questions)
    {
        if (request.Answers is null || request.Answers.Count == 0)
            throw new ArgumentException("Answers are required.");

        var questionLookup = questions.ToDictionary(q => q.QuestionId);
        var answeredQuestions = new HashSet<int>();
        var answers = new List<ValidatedAnswer>();
        var scoringAnswers = new List<OnboardingScoringAnswer>();
        var actionCodes = new List<string>();

        foreach (var answer in request.Answers)
        {
            if (!questionLookup.TryGetValue(answer.QuestionId, out var question))
                throw new ArgumentException($"Question {answer.QuestionId} is not valid.");

            if (!answeredQuestions.Add(answer.QuestionId))
                throw new ArgumentException($"Question {answer.QuestionId} was answered more than once.");

            if (answer.SelectedOptionIds is null || answer.SelectedOptionIds.Count == 0)
                throw new ArgumentException($"Question {answer.QuestionId} must include at least one option.");

            var distinctSelections = answer.SelectedOptionIds.Distinct().ToList();
            if (distinctSelections.Count != answer.SelectedOptionIds.Count)
                throw new ArgumentException($"Question {answer.QuestionId} includes duplicate selections.");

            var options = question.Options
                .Where(o => distinctSelections.Contains(o.OptionId) && o.IsActive)
                .OrderBy(o => o.DisplayOrder)
                .ToList();

            if (options.Count != distinctSelections.Count)
                throw new ArgumentException($"Question {answer.QuestionId} includes invalid option selections.");

            ValidateSelectionCount(question, options.Count);

            var scoringOptions = options
                .Select(option => new OnboardingScoringOption(
                    option.ScorePoints,
                    option.MetricModifiers
                        .Select(mod => new OnboardingScoringModifier(mod.Parameter, mod.ModifierValue))
                        .ToList()))
                .ToList();

            scoringAnswers.Add(new OnboardingScoringAnswer(question.Parameter, scoringOptions));

            if (!string.IsNullOrWhiteSpace(question.ConditionalActionsJson))
            {
                var conditional = JsonSerializer.Deserialize<Dictionary<int, string>>(question.ConditionalActionsJson!, JsonOptions);
                if (conditional is not null)
                {
                    foreach (var option in options)
                    {
                        if (conditional.TryGetValue(option.OptionId, out var code))
                            actionCodes.Add(code);
                    }
                }
            }

            answers.Add(new ValidatedAnswer(question, options));
        }

        var missingQuestions = questionLookup.Keys.Except(answeredQuestions).OrderBy(id => id).ToList();
        if (missingQuestions.Count > 0)
            throw new ArgumentException($"Missing answers for questions: {string.Join(", ", missingQuestions)}.");

        return (answers, scoringAnswers, actionCodes);
    }

    private static void ValidateSelectionCount(OnboardingQuestion question, int selectionCount)
    {
        if (string.Equals(question.InputControlType, "single_select", StringComparison.OrdinalIgnoreCase))
        {
            if (selectionCount != 1)
                throw new ArgumentException($"Question {question.QuestionId} requires exactly one selection.");
            return;
        }

        if (string.Equals(question.InputControlType, "multi_select", StringComparison.OrdinalIgnoreCase))
        {
            if (question.MaxAllowedSelections.HasValue && selectionCount > question.MaxAllowedSelections.Value)
                throw new ArgumentException($"Question {question.QuestionId} allows at most {question.MaxAllowedSelections} selections.");
            return;
        }

        throw new ArgumentException($"Question {question.QuestionId} has unsupported input control type '{question.InputControlType}'.");
    }

    private async Task<UserParameterSnapshot> LoadOrCreateUserSnapshotAsync(int userId, CancellationToken cancellationToken)
    {
        var snapshot = await db.UserParameterSnapshots.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (snapshot is not null)
            return snapshot;

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            throw new InvalidOperationException($"User {userId} not found.");

        snapshot = new UserParameterSnapshot
        {
            UserId = userId,
            UpdatedAt = DateTime.UtcNow
        };

        db.UserParameterSnapshots.Add(snapshot);
        logger.LogWarning("Snapshot missing for user {UserId}; creating new snapshot.", userId);
        return snapshot;
    }

    private async Task<OnboardingStatusSnapshot> GetStatusInternalAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var cached = await cache.GetOrCreateAsync(
            GetStatusCacheKey(userId),
            async () =>
            {
                var state = await db.UserOnboardingStates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

                if (state is null || !state.IsCompleted)
                    return new OnboardingStatusSnapshot(false, null, null);

                var result = await db.UserOnboardingResults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);

                var parameters = result is null
                    ? null
                    : new ParameterValues(result.Anx, result.Dep, result.Str, result.Slp,
                                          result.Soc, result.Cdt, result.Safe, result.Eng);

                return new OnboardingStatusSnapshot(true, state.CompletedAt, parameters);
            },
            TimeSpan.FromMinutes(2),
            cancellationToken);

        return cached;
    }

    private void InvalidateCaches(int userId, string locale)
    {
        cache.RemoveMany(GetQuestionsCacheKey(locale), GetStatusCacheKey(userId), $"users:{userId}", $"users:{userId}:parameters");
    }

    private static string GetQuestionsCacheKey(string locale) => $"onboarding:questions:{locale}";

    private static string GetStatusCacheKey(int userId) => $"onboarding:status:{userId}";

    private sealed record OnboardingStatusSnapshot(bool Completed, DateTime? CompletedAt, ParameterValues? Parameters);

    private sealed record ValidatedAnswer(OnboardingQuestion Question, List<OnboardingQuestionOption> SelectedOptions);
}
