using FluentValidation;

namespace api.Contracts.Users;

public class UpdateUserStatusRequestValidator : AbstractValidator<UpdateUserStatusRequest>
{
    public UpdateUserStatusRequestValidator()
    {
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("Status is required.");
    }
}
