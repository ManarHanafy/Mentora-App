namespace api.Contracts.AI;

public record AIAnalysisResponse(
    string RiskLevel,
    Dictionary<string, int> ParameterScores,
    Dictionary<string, int> Deltas,
    List<string> Tags
);
