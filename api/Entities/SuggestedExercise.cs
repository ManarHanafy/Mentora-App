namespace api.Entities;

/// <summary>Exercise recommendation returned by the AI for a user</summary>
public class SuggestedExercise
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? JournalEntryId { get; set; }

    /// <summary>Exercise code returned by the AI, e.g. "EX_ANX_01"</summary>
    public string ExerciseCode { get; set; } = string.Empty;

    /// <summary>Parameter code that triggered this recommendation</summary>
    public string Parameter { get; set; } = string.Empty;

    /// <summary>Actual parameter score at the time of suggestion</summary>
    public int Score { get; set; }

    /// <summary>Score range associated with this recommendation, e.g., "1–5"</summary>
    public string ScoreRange { get; set; } = string.Empty;

    // Navigation properties
    public User? User { get; set; }
    public JournalEntry? JournalEntry { get; set; }
}
