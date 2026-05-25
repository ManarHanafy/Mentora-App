namespace api.Entities;

public class OnboardingQuestionOption : AuditableEntity
{
    public int Id { get; set; }
    public int OnboardingQuestionId { get; set; }
    public int OptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int? ScorePoints { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public OnboardingQuestion? Question { get; set; }
    public ICollection<OnboardingOptionMetricModifier> MetricModifiers { get; set; } = new List<OnboardingOptionMetricModifier>();
}
