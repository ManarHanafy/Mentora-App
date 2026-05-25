namespace api.Entities;

public class OnboardingOptionMetricModifier : AuditableEntity
{
    public int Id { get; set; }
    public int OnboardingQuestionOptionId { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public int? ModifierValue { get; set; }
    public string? ModifierValueText { get; set; }

    public OnboardingQuestionOption? Option { get; set; }
}
