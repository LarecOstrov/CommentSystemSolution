using CommentSystem.Helpers;
using CommentSystem.Models.Inputs;
using FluentValidation;

public class AddCommentInputValidator : AbstractValidator<AddCommentInput>
{
    public AddCommentInputValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName is required.")
            .Matches(@"^[a-zA-Z0-9]+$").WithMessage("UserName can only contain letters and numbers.")
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(500)
            .Must(text => SanitizeInput.ClearText(text) == text)
            .WithMessage("Text contains invalid characters.");

        RuleFor(x => x.Captcha)
            .NotEmpty().WithMessage("Captcha is required.");
    }
}
