using api.Contracts.Onboarding;

namespace api.Services;

public interface IOnboardingService
{
    Task<OnboardingQuestionsResponse> GetQuestionsAsync(int userId, string? locale, CancellationToken cancellationToken = default);
    Task<OnboardingStatusResponse> GetStatusAsync(int userId, CancellationToken cancellationToken = default);
    Task<OnboardingSubmitResponse> SubmitAsync(int userId, SubmitOnboardingRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResetAsync(int adminUserId, int targetUserId, CancellationToken cancellationToken = default);
}
