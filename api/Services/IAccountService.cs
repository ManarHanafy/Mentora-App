using api.Abstractions;
using api.Contracts.Account;
using api.Contracts.Users;

namespace api.Services;

public interface IAccountService
{
    Task<Result<UserResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result>               ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result>               DeactivateAsync(int userId, CancellationToken cancellationToken = default);
}
