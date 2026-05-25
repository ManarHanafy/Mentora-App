using FluentValidation;

namespace api.Contracts.Authentication;

public class ResendEmailOtpRequestValidator : AbstractValidator<ResendEmailOtpRequest>
{
    public ResendEmailOtpRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
    }
}
