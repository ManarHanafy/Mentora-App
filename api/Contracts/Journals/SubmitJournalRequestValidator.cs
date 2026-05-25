using FluentValidation;

namespace api.Contracts.Journals;

public class SubmitJournalRequestValidator : AbstractValidator<SubmitJournalRequest>
{
    public SubmitJournalRequestValidator()
    {
        RuleFor(x => x.JournalText)
            .NotEmpty().WithMessage("Journal content is required.")
            .Length(1, 10000).WithMessage("Journal content must be between 1 and 10000 characters.");
    }
}
