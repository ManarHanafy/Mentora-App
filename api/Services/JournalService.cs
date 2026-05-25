using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using api.Persistence;
using api.Entities;
using api.Contracts.Journals;
using api.Contracts.AI;
using api.Contracts.Common;
using api.Contracts.Exercises;
using api.Infrastructure.Caching;

namespace api.Services;

public class JournalService(
    ApplicationDbContext db,
    IAIService ai,
    IAppCacheService cache,
    ILogger<JournalService> logger) : IJournalService
{
    private static readonly string[] RequiredParams = ["ANX", "DEP", "STR", "SLP", "SOC", "CDT", "SAFE", "ENG"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    public async Task<JournalResponse> SubmitAsync(int userId, SubmitJournalRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Incoming journal request. User={UserId}", userId);
        var snapshot = await LoadOrCreateUserSnapshotAsync(userId, cancellationToken);
        var snapshotScores = snapshot.ToParametersDictionary();
        var currentScores = RequiredParams.ToDictionary(
            p => p,
            p => snapshotScores.GetValueOrDefault(p.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        logger.LogInformation("Calling AI API for journal submit. User={UserId}", userId);
        var analysis = await ai.AnalyseAsync(request.JournalText, currentScores, cancellationToken);
        logger.LogInformation("Incoming AI response for submit. User={UserId} Risk={RiskLevel}", userId, analysis.Response.RiskLevel);
        ValidateAnalysisResponse(analysis.Response);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var entry = new JournalEntry
            {
                UserId = userId,
                JournalText = request.JournalText,
                RiskLevel = analysis.Response.RiskLevel,
                AiResponseJson = analysis.RawResponseJson
            };

            db.JournalEntries.Add(entry);
            logger.LogInformation("SaveChanges start: insert journal.");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("SaveChanges end: insert journal. JournalId={JournalId}", entry.Id);

            PersistAnalysis(entry.Id, userId, analysis.Response);
            snapshot.UpdateFromDictionary(analysis.Response.NewScores.ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => kv.Value));
            snapshot.LatestJournalEntryId = entry.Id;

            logger.LogInformation("SaveChanges start: insert journal children and snapshot.");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("SaveChanges end: insert journal children and snapshot.");

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            InvalidateCaches(userId);
            cache.Remove($"journals:{entry.Id}");
            logger.LogInformation("Transaction committed for journal submit. JournalId={JournalId}", entry.Id);
            return ToJournalResponse(analysis.Response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Journal submit failed. Rolling back transaction. User={UserId}", userId);
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            logger.LogInformation("Transaction rolled back for journal submit. User={UserId}", userId);
            throw;
        }
    }

    public async Task<PagedResponse<JournalSummaryResponse>> GetHistoryAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var baseQuery = db.JournalEntries
            .Where(e => e.UserId == userId)
            .Include(e => e.JournalTags)
            .OrderByDescending(e => e.CreatedAt)
            .AsNoTracking();

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select(e => new JournalSummaryResponse(
                e.Id,
                e.UserId,
                e.RiskLevel,
                e.JournalTags.Select(t => t.Tag).ToArray(),
                e.CreatedAt,
                e.UpdatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);
        return new PagedResponse<JournalSummaryResponse>(
            items,
            safePageNumber,
            safePageSize,
            totalCount,
            totalPages,
            safePageNumber < totalPages,
            safePageNumber > 1);
    }

    public async Task<JournalResponse?> GetByIdAsync(int journalEntryId, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            $"journals:{journalEntryId}",
            async () =>
            {
                var entry = await db.JournalEntries
                    .AsNoTracking()
                    .Include(e => e.JournalTags)
                    .Include(e => e.MatchedItems)
                        .ThenInclude(m => m.Details)
                    .Include(e => e.Score)
                    .Include(e => e.SuggestedExercises)
                    .FirstOrDefaultAsync(e => e.Id == journalEntryId, cancellationToken);

                if (entry is null)
                    return null;

                if (string.IsNullOrWhiteSpace(entry.AiResponseJson))
                {
                    logger.LogError("Stored AI response JSON is missing for journal {JournalId}", journalEntryId);
                    throw new InvalidOperationException("Stored journal analysis is unavailable.");
                }

                try
                {
                    var response = JsonSerializer.Deserialize<MentoraAnalyzeResponse>(entry.AiResponseJson, JsonOptions);
                    if (response is null)
                        throw new InvalidOperationException("Stored AI response is empty.");

                    ValidateAnalysisResponse(response);
                    return ToJournalResponse(response);
                }
                catch (Exception ex) when (ex is JsonException or InvalidOperationException)
                {
                    logger.LogError(ex, "Deserialization/validation failed for stored AI response. JournalId={JournalId}", journalEntryId);
                    throw new InvalidOperationException("Stored journal analysis is invalid.", ex);
                }
            },
            TimeSpan.FromMinutes(2),
            cancellationToken);
    }

    public async Task<JournalResponse?> UpdateAsync(int journalEntryId, UpdateJournalRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await db.JournalEntries
            .Include(e => e.JournalTags)
            .Include(e => e.MatchedItems)
                .ThenInclude(m => m.Details)
            .Include(e => e.Score)
            .Include(e => e.SuggestedExercises)
            .FirstOrDefaultAsync(e => e.Id == journalEntryId, cancellationToken);

        if (entry is null)
            return null;

        logger.LogInformation("Incoming journal update request. JournalId={JournalId}", journalEntryId);
        var snapshot = await LoadOrCreateUserSnapshotAsync(entry.UserId, cancellationToken);
        var snapshotScores = snapshot.ToParametersDictionary();
        var currentScores = RequiredParams.ToDictionary(
            p => p,
            p => snapshotScores.GetValueOrDefault(p.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        logger.LogInformation("Calling AI API for journal update. JournalId={JournalId}", journalEntryId);
        var analysis = await ai.AnalyseAsync(request.JournalText, currentScores, cancellationToken);
        logger.LogInformation("Incoming AI response for update. JournalId={JournalId} Risk={RiskLevel}", journalEntryId, analysis.Response.RiskLevel);
        ValidateAnalysisResponse(analysis.Response);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            if (!string.Equals(entry.JournalText, request.JournalText, StringComparison.Ordinal))
                entry.UpdatedAt = DateTime.UtcNow;

            entry.JournalText = request.JournalText;
            entry.RiskLevel = analysis.Response.RiskLevel;
            entry.AiResponseJson = analysis.RawResponseJson;

            db.JournalTags.RemoveRange(entry.JournalTags);
            db.MatchedItems.RemoveRange(entry.MatchedItems);
            db.SuggestedExercises.RemoveRange(entry.SuggestedExercises);
            if (entry.Score is not null)
                db.JournalScores.Remove(entry.Score);

            PersistAnalysis(entry.Id, entry.UserId, analysis.Response);
            snapshot.UpdateFromDictionary(analysis.Response.NewScores.ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => kv.Value));
            snapshot.LatestJournalEntryId = entry.Id;

            logger.LogInformation("SaveChanges start: update journal and replace analysis children. JournalId={JournalId}", journalEntryId);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("SaveChanges end: update journal and replace analysis children. JournalId={JournalId}", journalEntryId);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            InvalidateCaches(entry.UserId);
            cache.Remove($"journals:{journalEntryId}");
            logger.LogInformation("Transaction committed for journal update. JournalId={JournalId}", journalEntryId);
            return ToJournalResponse(analysis.Response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Journal update failed. Rolling back transaction. JournalId={JournalId}", journalEntryId);
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            logger.LogInformation("Transaction rolled back for journal update. JournalId={JournalId}", journalEntryId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int journalEntryId, CancellationToken cancellationToken = default)
    {
        var entry = await db.JournalEntries.FindAsync(new object[] { journalEntryId }, cancellationToken: cancellationToken);
        if (entry is null) return false;

        db.JournalEntries.Remove(entry);
        logger.LogInformation("SaveChanges start: delete journal. JournalId={JournalId}", journalEntryId);
        await db.SaveChangesAsync(cancellationToken);
        InvalidateCaches(entry.UserId);
        cache.Remove($"journals:{journalEntryId}");
        logger.LogInformation("SaveChanges end: delete journal. JournalId={JournalId}", journalEntryId);
        return true;
    }

    private void InvalidateCaches(int userId)
    {
        cache.RemoveMany($"users:{userId}:parameters", $"stats:{userId}", $"journals:{userId}:history");
    }

    private void PersistAnalysis(int journalId, int userId, MentoraAnalyzeResponse response)
    {
        var scores = response.NewScores.ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        db.JournalScores.Add(new JournalScore
        {
            JournalEntryId = journalId,
            Anx = scores["ANX"],
            Dep = scores["DEP"],
            Str = scores["STR"],
            Slp = scores["SLP"],
            Soc = scores["SOC"],
            Cdt = scores["CDT"],
            Safe = scores["SAFE"],
            Eng = scores["ENG"]
        });

        foreach (var tag in response.Tags)
        {
            db.JournalTags.Add(new JournalTag
            {
                JournalEntryId = journalId,
                Tag = tag
            });
        }

        foreach (var group in response.MatchedItems)
        {
            var matchedItem = new MatchedItem
            {
                JournalEntryId = journalId,
                Parameter = group.Parameter,
                Reason = group.Reason
            };

            foreach (var item in group.Items)
            {
                matchedItem.Details.Add(new MatchedItemDetail
                {
                    ItemId = item.Id,
                    Intensity = item.Intensity03,
                    MatchText = item.MatchText
                });
            }

            db.MatchedItems.Add(matchedItem);
        }

        var normalizedSuggestions = response.SuggestedExercises
            .GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        foreach (var suggestion in normalizedSuggestions)
        {
            if (string.IsNullOrWhiteSpace(suggestion.Id))
                continue;

            var exerciseCode = suggestion.Id.Trim();
            var parameter = suggestion.Parameter?.Trim().ToLowerInvariant() ?? string.Empty;
            var scoreRange = suggestion.ScoreRange?.Trim() ?? string.Empty;

            var existingSuggestion = db.SuggestedExercises
                .FirstOrDefault(se => se.UserId == userId && se.ExerciseCode == exerciseCode);

            if (existingSuggestion is null)
            {
                db.SuggestedExercises.Add(new SuggestedExercise
                {
                    UserId = userId,
                    JournalEntryId = journalId,
                    ExerciseCode = exerciseCode,
                    Parameter = parameter,
                    Score = suggestion.Score,
                    ScoreRange = scoreRange
                });
                continue;
            }

            existingSuggestion.JournalEntryId = journalId;
            existingSuggestion.Parameter = parameter;
            existingSuggestion.Score = suggestion.Score;
            existingSuggestion.ScoreRange = scoreRange;
        }
    }

    private async Task<UserParameterSnapshot> LoadOrCreateUserSnapshotAsync(int userId, CancellationToken ct)
    {
        var snapshot = await db.UserParameterSnapshots.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (snapshot is not null)
            return snapshot;

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, ct);
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

    private static void ValidateAnalysisResponse(MentoraAnalyzeResponse response)
    {
        if (response.MatchedItems is null || response.Deltas is null || response.NewScores is null ||
            response.Tags is null || response.SuggestedExercises is null || string.IsNullOrWhiteSpace(response.RiskLevel))
        {
            throw new InvalidOperationException("AI response is missing required fields.");
        }

        var deltaKeys = response.Deltas.Keys.Select(k => k.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var scoreKeys = response.NewScores.Keys.Select(k => k.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (RequiredParams.Any(p => !deltaKeys.Contains(p)) || RequiredParams.Any(p => !scoreKeys.Contains(p)))
            throw new InvalidOperationException("AI response must include all 8 parameter keys in deltas and new_scores.");
    }

    private static JournalResponse ToJournalResponse(MentoraAnalyzeResponse response) =>
        new(
            response.MatchedItems
                .Select(group => new MatchedItemResponse(
                    group.Parameter,
                    group.Items.Select(item => new MatchedItemEntryResponse(
                        item.Id,
                        item.Intensity03,
                        item.MatchText)).ToList(),
                    group.Reason))
                .ToList(),
            response.Deltas,
            response.NewScores,
            response.Tags,
            response.RiskLevel,
            response.SuggestedExercises
                .Select(ex => new SuggestedExerciseResponse(
                    ex.Id,
                    ex.Parameter,
                    ex.Score,
                    ex.ScoreRange))
                .ToList());

}
