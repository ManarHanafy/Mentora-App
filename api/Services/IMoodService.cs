using api.Contracts.Moods;

namespace api.Services;

public interface IMoodService
{
    Task<MoodResponse> SubmitAsync(int userId, int mood, DateOnly date, CancellationToken cancellationToken = default);
    Task<MoodResponse?> GetByDateAsync(int userId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<MoodResponse>> GetHistoryAsync(int userId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
