namespace api.Contracts.AI;

public record AnalysisResultResponse(
    string RiskLevel,
    Dictionary<string, int> CurrentScores,
    Dictionary<string, int> Deltas,
    List<string> Tags
);
