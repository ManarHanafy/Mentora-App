namespace api.Contracts.Crisis;

public record CrisisResourcesResponse(
    string Message,
    List<CrisisResource> Resources,
    string ImmediateAdvice
);

public record CrisisResource(
    string Name,
    string Type,
    string Contact,
    string Description,
    bool Available24Hours
);
