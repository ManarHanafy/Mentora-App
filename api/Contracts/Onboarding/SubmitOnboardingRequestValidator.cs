using FluentValidation;

namespace api.Contracts.Onboarding;

public class SubmitOnboardingRequestValidator : AbstractValidator<SubmitOnboardingRequest>
{
    public SubmitOnboardingRequestValidator()
    {
        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Answers are required.")
            .Must(list => list.Count > 0).WithMessage("At least one answer is required.");

        RuleForEach(x => x.Answers).SetValidator(new OnboardingAnswerRequestValidator());
    }
}

public class OnboardingAnswerRequestValidator : AbstractValidator<OnboardingAnswerRequest>
{
    public OnboardingAnswerRequestValidator()
    {
        RuleFor(x => x.QuestionId)
            .GreaterThan(0).WithMessage("QuestionId must be greater than 0.");

        RuleFor(x => x.SelectedOptionIds)
            .NotNull().WithMessage("SelectedOptionIds are required.")
            .Must(list => list.Count > 0).WithMessage("At least one option must be selected.");
    }
}
