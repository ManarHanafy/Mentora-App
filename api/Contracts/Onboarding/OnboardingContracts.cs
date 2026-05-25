using api.Contracts.Users;

namespace api.Contracts.Onboarding;

public record OnboardingQuestionsResponse(
    bool Completed,
    DateTime? CompletedAt,
    bool ShouldShow,
    string Locale,
    List<OnboardingQuestionResponse> Questions
);

public record OnboardingQuestionResponse(
    int QuestionId,
    string Category,
    string Parameter,
    string QuestionText,
    string InputControlType,
    List<OnboardingOptionResponse> ResponseOptions,
    string? ScoringNote,
    int? MaxAllowedSelections,
    bool IsSensitiveQuestion,
    string? PreQuestionDisclaimer,
    Dictionary<int, OnboardingActionMetadata>? ConditionalActions
);

public record OnboardingOptionResponse(
    int OptionId,
    string OptionText,
    int? ScorePoints,
    Dictionary<string, object>? MetricModifiers
);

public record OnboardingActionMetadata(
    string Code,
    string Type,
    string Severity,
    List<string>? Flags = null
);

public record OnboardingSubmitResponse(
    bool Success,
    bool Completed,
    DateTime? CompletedAt,
    ParameterValues Parameters,
    List<OnboardingActionMetadata> Actions
);

public record OnboardingStatusResponse(
    bool Completed,
    DateTime? CompletedAt,
    bool ShouldShow,
    ParameterValues? Parameters
);

public record SubmitOnboardingRequest(
    List<OnboardingAnswerRequest> Answers,
    string? Locale = null
);

public record OnboardingAnswerRequest(
    int QuestionId,
    List<int> SelectedOptionIds
);
