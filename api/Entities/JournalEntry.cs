namespace api.Entities;

/// <summary>Journal entry domain model</summary>
public class JournalEntry : AuditableEntity
{
    public int      Id        { get; set; }
    public int      UserId    { get; set; }
    public string   JournalText   { get; set; } = string.Empty;
    public string   AiResponseJson { get; set; } = string.Empty;

    /// <summary>Risk level: normal | elevated | crisis</summary>
    public string       RiskLevel { get; set; } = "normal";

    // Navigation properties
    public User?                            User              { get; set; }
    public ICollection<JournalTag>          JournalTags       { get; set; } = new List<JournalTag>();
    public ICollection<MatchedItem>         MatchedItems      { get; set; } = new List<MatchedItem>();
    public JournalScore?                    Score             { get; set; }
    public ICollection<SuggestedExercise>   SuggestedExercises { get; set; } = new List<SuggestedExercise>();
}
