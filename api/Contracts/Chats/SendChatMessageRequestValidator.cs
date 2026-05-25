using FluentValidation;

namespace api.Contracts.Chats;

public class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message cannot be empty.")
            .MaximumLength(5000).WithMessage("Message must be 5000 characters or less.");
    }
}
