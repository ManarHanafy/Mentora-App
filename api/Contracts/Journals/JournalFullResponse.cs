namespace api.Contracts.Journals;

using api.Contracts.Exercises;
using api.Contracts.AI;

public record JournalFullResponse(
    int Id,
    int UserId,
    string JournalText,
    string RiskLevel,
    List<string> Tags,
    List<MatchedItemResponse> MatchedItems,
    Dictionary<string, int> Scores,
    List<SuggestedExerciseResponse> SuggestedExercises,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
