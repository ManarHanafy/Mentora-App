namespace api.Contracts.AI;

public record TextAnalysisRequest(
    int UserId,
    string Text,
    Dictionary<string, int> CurrentScores
);
