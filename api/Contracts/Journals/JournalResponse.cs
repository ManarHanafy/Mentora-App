namespace api.Contracts.Journals;

using api.Contracts.Exercises;
using api.Contracts.AI;
using System.Text.Json.Serialization;

public record JournalResponse(
    [property: JsonPropertyName("matched_items")] List<MatchedItemResponse> MatchedItems,
    [property: JsonPropertyName("deltas")] Dictionary<string, int> Deltas,
    [property: JsonPropertyName("new_scores")] Dictionary<string, int> NewScores,
    [property: JsonPropertyName("tags")] List<string> Tags,
    [property: JsonPropertyName("risk_level")] string RiskLevel,
    [property: JsonPropertyName("suggested_exercises")] List<SuggestedExerciseResponse> SuggestedExercises
);
