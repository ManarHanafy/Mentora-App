namespace api.Contracts.Exercises;

public record UpdateExerciseRequest(
    string Parameter,
    int    Score,
    string ScoreRange
);
