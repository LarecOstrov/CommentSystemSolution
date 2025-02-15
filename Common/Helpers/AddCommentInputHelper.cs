using Common.Models.Inputs;
using FluentValidation;

namespace Common.Helpers;

public class AddCommentInputHelper : AbstractValidator<CommentInput>
{
    public AddCommentInputHelper()
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
            .Must(text =>
            {
                var sanitizedText = SanitizeInputHelper.ClearText(text);

                return sanitizedText.Replace("\r\n", "\n") == text.Replace("\r\n", "\n");
            })
            .WithMessage(x =>
            {
                var sanitizedText = SanitizeInputHelper.ClearText(x.Text);
                var invalidChars = x.Text.Except(sanitizedText).Distinct();
                return $"Text contains invalid characters: {string.Join(", ", invalidChars)}";
            });

        RuleFor(x => x.Captcha)
            .NotEmpty().WithMessage("Captcha is required.");
    }
}
