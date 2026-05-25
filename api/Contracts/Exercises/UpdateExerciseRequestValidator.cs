using FluentValidation;

namespace api.Contracts.Exercises;

public class UpdateExerciseRequestValidator : AbstractValidator<UpdateExerciseRequest>
{
    public UpdateExerciseRequestValidator()
    {
        RuleFor(x => x.Parameter)
            .NotEmpty().WithMessage("Parameter is required.");

        RuleFor(x => x.Score)
            .GreaterThanOrEqualTo(0).WithMessage("Score must be greater than or equal to 0.");

        RuleFor(x => x.ScoreRange)
            .NotEmpty().WithMessage("ScoreRange is required.")
            .MaximumLength(20).WithMessage("ScoreRange must be at most 20 characters.");
    }
}
