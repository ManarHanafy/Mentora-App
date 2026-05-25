using api.Contracts.Crisis;
using Microsoft.Extensions.Options;

namespace api.Services;

public interface ICrisisResourceService
{
    CrisisResourcesResponse GetResources(string? locale, string? countryCode);
}

public sealed class CrisisResourceService(IOptions<CrisisResourcesOptions> options) : ICrisisResourceService
{
    private readonly CrisisResourcesOptions _options = options.Value;

    public CrisisResourcesResponse GetResources(string? locale, string? countryCode)
    {
        var normalizedLocale = NormalizeLocale(locale, countryCode);

        if (_options.Locales.TryGetValue(normalizedLocale, out var localized))
            return localized;

        if (_options.Locales.TryGetValue(_options.DefaultLocale, out var fallback))
            return fallback;

        return GetBuiltInFallback();
    }

    private static string NormalizeLocale(string? locale, string? countryCode)
    {
        if (!string.IsNullOrWhiteSpace(locale))
            return locale.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(countryCode))
            return countryCode.Trim().ToLowerInvariant();

        return "global";
    }

    private static CrisisResourcesResponse GetBuiltInFallback() =>
        new(
            Message: "You are not alone. Immediate help is available. Please reach out to one of the resources below.",
            Resources:
            [
                new CrisisResource(
                    Name: "International Association for Suicide Prevention",
                    Type: "Hotline Directory",
                    Contact: "https://www.iasp.info/resources/Crisis_Centres/",
                    Description: "Find a crisis centre in your country.",
                    Available24Hours: true),
                new CrisisResource(
                    Name: "Emergency Services",
                    Type: "Emergency",
                    Contact: "911 / 999 / 112 (local emergency number)",
                    Description: "Contact local emergency services immediately if you are in danger.",
                    Available24Hours: true)
            ],
            ImmediateAdvice: "If you are in immediate danger, call your local emergency number now and seek support from someone you trust.");
}
