using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace api.HealthChecks;

public class AIServiceHealthCheck(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = configuration["MentoraAI:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                return HealthCheckResult.Unhealthy("MentoraAI:BaseUrl is not configured.");

            using var request = new HttpRequestMessage(HttpMethod.Head, baseUrl);
            var client = httpClientFactory.CreateClient(nameof(AIServiceHealthCheck));
            using var response = await client.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("AI endpoint is reachable.")
                : HealthCheckResult.Degraded($"AI endpoint returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("AI endpoint health check failed.", ex);
        }
    }
}
