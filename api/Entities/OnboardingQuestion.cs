namespace api.Entities;

public class OnboardingQuestion : AuditableEntity
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Locale { get; set; } = "en";
    public string Category { get; set; } = string.Empty;
    public string Parameter { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string InputControlType { get; set; } = string.Empty;
    public string? ScoringNote { get; set; }
    public int? MaxAllowedSelections { get; set; }
    public bool IsSensitiveQuestion { get; set; }
    public string? PreQuestionDisclaimer { get; set; }
    public string? ConditionalActionsJson { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<OnboardingQuestionOption> Options { get; set; } = new List<OnboardingQuestionOption>();
}
