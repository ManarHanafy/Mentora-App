namespace api.Contracts.Journals;

public record JournalSummaryResponse(
    int Id,
    int UserId,
    string RiskLevel,
    string[] Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
