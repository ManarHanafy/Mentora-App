using FluentValidation;

namespace api.Contracts.Account;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("New password must not exceed 100 characters.")
            .Matches("[A-Za-z]").WithMessage("New password must contain at least one letter.")
            .Matches("^[^\\s]+$").WithMessage("New password must not contain spaces.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from current password.");
    }
}
