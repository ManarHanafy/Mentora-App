using System.Text.Json.Serialization;

namespace api.Contracts.AI;

public record MentoraAnalyzeRequest(
    [property: JsonPropertyName("journal_text")] string JournalText,
    [property: JsonPropertyName("current_scores")] Dictionary<string, int> CurrentScores
);

public record MentoraMatchedItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("intensity_0_3")] int Intensity03,
    [property: JsonPropertyName("match_text")] string MatchText
);

public record MentoraMatchedGroup(
    [property: JsonPropertyName("parameter")] string Parameter,
    [property: JsonPropertyName("items")] List<MentoraMatchedItem> Items,
    [property: JsonPropertyName("reason")] string Reason
);

public record MentoraSuggestedExercise(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("parameter")] string Parameter,
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("score_range")] string ScoreRange
);

public record MentoraAnalyzeResponse(
    [property: JsonPropertyName("matched_items")] List<MentoraMatchedGroup> MatchedItems,
    [property: JsonPropertyName("deltas")] Dictionary<string, int> Deltas,
    [property: JsonPropertyName("new_scores")] Dictionary<string, int> NewScores,
    [property: JsonPropertyName("tags")] List<string> Tags,
    [property: JsonPropertyName("risk_level")] string RiskLevel,
    [property: JsonPropertyName("suggested_exercises")] List<MentoraSuggestedExercise> SuggestedExercises
);
