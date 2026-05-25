namespace api.Contracts.Statistics;

public record UserStatisticsResponse(
    int UserId,
    int TotalJournalEntries,
    string LatestRiskLevel,
    DateTime? LastJournalDate,
    RiskDistribution RiskDistribution,
    Dictionary<string, int> CurrentScores,
    List<ParameterSummary> ParameterInsights
);

public record RiskDistribution(
    int Normal,
    int Elevated,
    int High,
    int Crisis
);

public record ParameterSummary(
    string Parameter,
    int CurrentScore,
    int Delta,
    string Trend
);
