namespace api.Contracts.Exercises;

using System.Text.Json.Serialization;

public record SuggestedExerciseResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("parameter")] string Parameter,
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("score_range")] string ScoreRange
);
