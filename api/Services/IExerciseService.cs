using api.Contracts.Exercises;

namespace api.Services;

public interface IExerciseService
{
    Task<IEnumerable<ExerciseResponse>> GetAllAsync(int userId, string? parameter, CancellationToken cancellationToken = default);
    Task<ExerciseResponse?> GetByIdAsync(int userId, int exerciseId, CancellationToken cancellationToken = default);
    Task<ExerciseResponse?> UpdateAsync(int userId, int exerciseId, UpdateExerciseRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int userId, int exerciseId, CancellationToken cancellationToken = default);
    Task<int> DeleteByJournalAsync(int userId, int journalId, CancellationToken cancellationToken = default);
}
