using api.Abstractions;
using api.Contracts.Statistics;

namespace api.Services;

public interface IStatisticsService
{
    Task<Result<UserStatisticsResponse>> GetUserStatisticsAsync(int userId, CancellationToken cancellationToken = default);
}
