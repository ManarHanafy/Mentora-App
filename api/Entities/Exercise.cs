namespace api.Entities;

/// <summary>Master library of therapeutic exercises</summary>
public class Exercise
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Exercise type: CBT, Breathing, Sleep, Behavioral, Relaxation, Social, Safety, Mindfulness</summary>
    public string ExerciseType    { get; set; } = string.Empty;
    public int    DurationMinutes { get; set; }

    /// <summary>Difficulty level: beginner, intermediate, advanced</summary>
    public string Difficulty { get; set; } = "beginner";

    /// <summary>Applicable parameters stored as comma-separated string</summary>
    public List<string> ApplicableParameters { get; set; } = new();

    public string   Instructions { get; set; } = string.Empty;
    public bool     IsActive     { get; set; } = true;
    public DateTime CreatedAt    { get; set; }
}
