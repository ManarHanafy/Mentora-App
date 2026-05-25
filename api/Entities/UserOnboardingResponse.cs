namespace api.Entities;

public class UserOnboardingResponse : AuditableEntity
{
    public int Id { get; set; }
    public int UserOnboardingStateId { get; set; }
    public int UserId { get; set; }
    public int OnboardingQuestionId { get; set; }
    public int QuestionId { get; set; }
    public string LocaleSnapshot { get; set; } = "en";
    public string CategorySnapshot { get; set; } = string.Empty;
    public string ParameterSnapshot { get; set; } = string.Empty;
    public string QuestionTextSnapshot { get; set; } = string.Empty;
    public string InputControlTypeSnapshot { get; set; } = string.Empty;
    public string? ScoringNoteSnapshot { get; set; }
    public int? MaxAllowedSelectionsSnapshot { get; set; }
    public bool IsSensitiveQuestionSnapshot { get; set; }
    public string? PreQuestionDisclaimerSnapshot { get; set; }
    public string? ConditionalActionsSnapshotJson { get; set; }

    public UserOnboardingState? State { get; set; }
    public OnboardingQuestion? Question { get; set; }
    public ICollection<UserOnboardingResponseOption> SelectedOptions { get; set; } = new List<UserOnboardingResponseOption>();
}
