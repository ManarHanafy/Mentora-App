using Microsoft.EntityFrameworkCore;
using api.Abstractions;
using api.Contracts.Statistics;
using api.Errors;
using api.Infrastructure.Caching;
using api.Persistence;

namespace api.Services;

public class StatisticsService(
    ApplicationDbContext db,
    IAppCacheService cache,
    ILogger<StatisticsService> logger) : IStatisticsService
{
    private static readonly string[] AllParams = ["anx", "dep", "str", "slp", "soc", "cdt", "safe", "eng"];

    public async Task<Result<UserStatisticsResponse>> GetUserStatisticsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return Result.Failure<UserStatisticsResponse>(UserErrors.NotFound);

        var snapshot = await db.UserParameterSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var latest = await db.JournalEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.RiskLevel, e.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        var cacheKey = string.Join(':',
            "stats",
            userId,
            snapshot?.UpdatedAt.Ticks ?? 0,
            latest?.CreatedAt.Ticks ?? 0,
            latest?.RiskLevel ?? "none");
        var stats = await cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var userEntries = db.JournalEntries
                    .Where(e => e.UserId == userId)
                    .AsNoTracking();

                var currentScores = snapshot?.ToParametersDictionary()
                    ?? AllParams.ToDictionary(p => p, _ => 0);

                var total = await userEntries.CountAsync(cancellationToken);
                var latestRisk = latest?.RiskLevel ?? "normal";
                var lastDate = latest?.CreatedAt;

                var riskCounts = await userEntries
                    .GroupBy(e => e.RiskLevel)
                    .Select(g => new { Risk = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Risk, x => x.Count, cancellationToken);

                var riskDist = new RiskDistribution(
                    Normal: riskCounts.GetValueOrDefault("normal"),
                    Elevated: riskCounts.GetValueOrDefault("elevated"),
                    High: riskCounts.GetValueOrDefault("high"),
                    Crisis: riskCounts.GetValueOrDefault("crisis"));

                var firstScore = await db.JournalScores
                    .AsNoTracking()
                    .Where(s => s.JournalEntry!.UserId == userId)
                    .OrderBy(s => s.JournalEntry!.CreatedAt)
                    .Select(s => new FirstScoreSnapshot(s.Anx, s.Dep, s.Str, s.Slp, s.Soc, s.Cdt, s.Safe, s.Eng))
                    .FirstOrDefaultAsync(cancellationToken);

                var insights = BuildInsights(firstScore, currentScores);

                return new UserStatisticsResponse(
                    userId,
                    total,
                    latestRisk,
                    lastDate,
                    riskDist,
                    currentScores,
                    insights);
            },
            TimeSpan.FromMinutes(3),
            cancellationToken);

        logger.LogInformation("Statistics retrieved for user {UserId}", userId);
        return Result.Success(stats);
    }

    private static List<ParameterSummary> BuildInsights(
        FirstScoreSnapshot? firstScore,
        Dictionary<string, int> currentScores)
    {
        var firstEntryScores = firstScore is null
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["anx"] = firstScore.Anx,
                ["dep"] = firstScore.Dep,
                ["str"] = firstScore.Str,
                ["slp"] = firstScore.Slp,
                ["soc"] = firstScore.Soc,
                ["cdt"] = firstScore.Cdt,
                ["safe"] = firstScore.Safe,
                ["eng"] = firstScore.Eng
            };

        return AllParams.Select(param =>
        {
            var first   = firstEntryScores.TryGetValue(param, out var f) ? f : 0;
            var current = currentScores.TryGetValue(param, out var c)    ? c : 0;
            var delta   = current - first;
            var trend   = delta > 0 ? "up" : delta < 0 ? "down" : "stable";

            return new ParameterSummary(param, current, delta, trend);
        }).ToList();
    }

    private sealed record FirstScoreSnapshot(
        int Anx,
        int Dep,
        int Str,
        int Slp,
        int Soc,
        int Cdt,
        int Safe,
        int Eng);
}
