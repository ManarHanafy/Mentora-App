namespace api.Entities;

public class UserOnboardingResponseOption : AuditableEntity
{
    public int Id { get; set; }
    public int UserOnboardingResponseId { get; set; }
    public int OnboardingQuestionOptionId { get; set; }
    public int OptionId { get; set; }
    public string OptionTextSnapshot { get; set; } = string.Empty;
    public int? ScorePointsSnapshot { get; set; }
    public string? MetricModifiersSnapshotJson { get; set; }

    public UserOnboardingResponse? Response { get; set; }
    public OnboardingQuestionOption? QuestionOption { get; set; }
}
