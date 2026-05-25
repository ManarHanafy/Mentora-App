using FluentValidation;

namespace api.Contracts.Common;

public class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SortDirection)
            .Must(v => string.IsNullOrWhiteSpace(v) || v.Equals("asc", StringComparison.OrdinalIgnoreCase) || v.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either 'asc' or 'desc'.");

        RuleFor(x => x.IsActive)
            .Must(v => v is null || v is true || v is false)
            .WithMessage("IsActive must be true or false when provided.");

        RuleFor(x => x.EmailVerified)
            .Must(v => v is null || v is true || v is false)
            .WithMessage("EmailVerified must be true or false when provided.");
    }
}
