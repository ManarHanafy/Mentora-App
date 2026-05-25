namespace api.Contracts.Crisis;

public sealed class CrisisResourcesOptions
{
    public const string SectionName = "CrisisResources";

    public string DefaultLocale { get; init; } = "global";

    public Dictionary<string, CrisisResourcesResponse> Locales { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
