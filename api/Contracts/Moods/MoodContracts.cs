namespace api.Contracts.Moods;

public record SubmitMoodRequest(int Mood);

public record MoodResponse(
    int Id,
    DateOnly Date,
    int Mood
);
