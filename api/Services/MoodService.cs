using api.Contracts.Moods;

namespace api.Services;

public class MoodService(ApplicationDbContext db) : IMoodService
{
    public async Task<MoodResponse> SubmitAsync(int userId, int mood, DateOnly date, CancellationToken cancellationToken = default)
    {
        if (mood is < 1 or > 5)
            throw new ArgumentException("Mood must be between 1 and 5.");

        var entry = await db.MoodEntries
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Date == date, cancellationToken);

        if (entry is not null)
            throw new InvalidOperationException("Mood entry for this date already exists.");

        entry = new MoodEntry
        {
            UserId = userId,
            Date = date,
            Mood = mood
        };
        db.MoodEntries.Add(entry);

        await db.SaveChangesAsync(cancellationToken);
        return new MoodResponse(entry.Id, entry.Date, entry.Mood);
    }

    public async Task<MoodResponse?> GetByDateAsync(int userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var entry = await db.MoodEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Date == date, cancellationToken);

        return entry is null ? null : new MoodResponse(entry.Id, entry.Date, entry.Mood);
    }

    public async Task<List<MoodResponse>> GetHistoryAsync(
        int userId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var endDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = from ?? endDate.AddDays(-29);

        return await db.MoodEntries
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= startDate && m.Date <= endDate)
            .OrderBy(m => m.Date)
            .Select(m => new MoodResponse(m.Id, m.Date, m.Mood))
            .ToListAsync(cancellationToken);
    }
}
