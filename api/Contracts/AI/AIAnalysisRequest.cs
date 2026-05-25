namespace api.Contracts.AI;

public record AIAnalysisRequest(
    int UserId,
    int JournalEntryId,
    string JournalText,
    Dictionary<string, int> CurrentScores
);
