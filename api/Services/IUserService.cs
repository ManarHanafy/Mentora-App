using api.Abstractions;
using api.Contracts.Common;
using api.Contracts.Users;

namespace api.Services;

public interface IUserService
{
    Task<(UserResponse? response, string? error)> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByIdForAdminAsync(int userId, CancellationToken cancellationToken = default);
    Task<PagedResponse<UserResponse>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<UserParametersResponse?> GetParametersAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result> UpdateRoleAsync(int userId, string role, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
}
