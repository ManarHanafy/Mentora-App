namespace api.Contracts.Journals;

public record ParameterTrendResponse(
    string Parameter,
    List<TrendPoint> Points
);

public record TrendPoint(DateTime Timestamp, int Value);
