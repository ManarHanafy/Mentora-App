using Mapster;
using Microsoft.EntityFrameworkCore;
using api.Persistence;
using api.Contracts.Exercises;

namespace api.Services;

public class ExerciseService(ApplicationDbContext db) : IExerciseService
{
    private static readonly HashSet<string> ValidParameters =
        new(StringComparer.OrdinalIgnoreCase) { "anx", "dep", "str", "slp", "soc", "cdt", "safe", "eng" };

    public async Task<IEnumerable<ExerciseResponse>> GetAllAsync(int userId, string? parameter, CancellationToken cancellationToken = default)
    {
        var query = db.SuggestedExercises
            .Where(se => se.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameter))
        {
            var p = parameter.Trim().ToLowerInvariant();
            query = query.Where(se => se.Parameter == p);
        }

        var exercises = await query
            .OrderByDescending(se => se.Id)
            .ToListAsync(cancellationToken);

        return exercises.Adapt<List<ExerciseResponse>>();
    }

    public async Task<ExerciseResponse?> GetByIdAsync(int userId, int exerciseId, CancellationToken cancellationToken = default)
    {
        var exercise = await db.SuggestedExercises
            .FirstOrDefaultAsync(se => se.Id == exerciseId && se.UserId == userId, cancellationToken);

        return exercise?.Adapt<ExerciseResponse>();
    }

    public async Task<ExerciseResponse?> UpdateAsync(int userId, int exerciseId, UpdateExerciseRequest request, CancellationToken cancellationToken = default)
    {
        var exercise = await db.SuggestedExercises
            .FirstOrDefaultAsync(se => se.Id == exerciseId && se.UserId == userId, cancellationToken);

        if (exercise is null)
            return null;

        exercise.Parameter = (request.Parameter ?? string.Empty).Trim().ToUpperInvariant();
        exercise.Score = request.Score;
        exercise.ScoreRange = (request.ScoreRange ?? string.Empty).Trim();
        await db.SaveChangesAsync(cancellationToken);

        return exercise.Adapt<ExerciseResponse>();
    }

    public async Task<bool> DeleteAsync(int userId, int exerciseId, CancellationToken cancellationToken = default)
    {
        var exercise = await db.SuggestedExercises
            .FirstOrDefaultAsync(se => se.Id == exerciseId && se.UserId == userId, cancellationToken);

        if (exercise is null)
            return false;

        db.SuggestedExercises.Remove(exercise);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DeleteByJournalAsync(int userId, int journalId, CancellationToken cancellationToken = default)
    {
        var exercises = await db.SuggestedExercises
            .Where(se => se.JournalEntryId == journalId && se.UserId == userId)
            .ToListAsync(cancellationToken);

        if (exercises.Count == 0)
            return 0;

        db.SuggestedExercises.RemoveRange(exercises);
        await db.SaveChangesAsync(cancellationToken);
        return exercises.Count;
    }

    public static bool IsValidParameter(string parameter) =>
        ValidParameters.Contains(parameter);
}
