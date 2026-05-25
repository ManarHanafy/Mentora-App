namespace api.Contracts.Exercises;

public record ExerciseResponse(
    int Id,
    string ExerciseCode,
    string Parameter,
    int Score,
    string ScoreRange,
    int? JournalEntryId
);
