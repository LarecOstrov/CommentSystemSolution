using Common.Models.Inputs;
using FluentValidation;
using System.Text.RegularExpressions;

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
                var originalText = x.Text;
                var sanitizedText = SanitizeInputHelper.ClearText(x.Text);
                var invalidTags = ExtractRemovedTags(originalText, sanitizedText);
                if (invalidTags.Count > 0)
                {
                    return $"Text contains invalid tags: {string.Join(", ", invalidTags)}";
                }
                return "";
            })
            .Must(text => HasValidHtmlStructure(text, out _))
            .WithMessage(x =>
            {
                HasValidHtmlStructure(x.Text, out var errorMessage);
                return $"Invalid html tags structure: {errorMessage}";
            })
            .Must(text => HasValidBBCodeStructure(text, out _))
            .WithMessage(x =>
            {
                HasValidBBCodeStructure(x.Text, out var errorMessage);
                return $"Invalid BBCode structure: {errorMessage}";
            }); 

        RuleFor(x => x.Captcha)
            .NotEmpty().WithMessage("Captcha is required.");
    }

    /// <summary>
    /// Determines which tags were removed during sanitization.
    /// </summary>
    private static List<string> ExtractRemovedTags(string original, string sanitized)
    {
        var tagRegex = new Regex(@"<\s*(/?)\s*([a-zA-Z0-9]+)[^>]*>", RegexOptions.IgnoreCase);
        var originalTags = tagRegex.Matches(original).Select(m => m.Value).ToList();
        var sanitizedTags = tagRegex.Matches(sanitized).Select(m => m.Value).ToList();

        return originalTags.Except(sanitizedTags).Distinct().ToList();
    }

    /// <summary>
    /// Checks if the html tags structure is valid.
    /// </summary>
    private static bool HasValidHtmlStructure(string input, out string errorMessage)
    {
        var allowedTags = new HashSet<string> { "a", "code", "i", "strong" };
        var tagRegex = new Regex(@"<(/?)(\w+)[^>]*>", RegexOptions.IgnoreCase);
        var stack = new Stack<string>();

        foreach (Match match in tagRegex.Matches(input))
        {
            string tag = match.Groups[2].Value.ToLower();
            bool isClosing = match.Groups[1].Value == "/";

            if (!allowedTags.Contains(tag))
            {
                errorMessage = $"Tag <{tag}> is not allowed.";
                return false;
            }

            if (isClosing)
            {
                if (stack.Count == 0 || stack.Peek() != tag)
                {
                    errorMessage = $"Closing tag </{tag}> is incorrect or not matching.";
                    return false;
                }
                stack.Pop();
            }
            else
            {
                stack.Push(tag);
            }
        }

        if (stack.Count > 0)
        {
            errorMessage = $"Unclosed tag <{stack.Peek()}> found.";
            return false;
        }

        errorMessage = "";
        return true;
    }

    /// <summary>
    /// Checks if the bbcode tags structure is valid.
    /// </summary>
    private static bool HasValidBBCodeStructure(string input, out string errorMessage)
    {
        var allowedTags = new HashSet<string> { "a", "code", "i", "strong" };
        var tagRegex = new Regex(@"\[(\/?)(\w+)[^\]]*\]", RegexOptions.IgnoreCase);
        var stack = new Stack<string>();

        foreach (Match match in tagRegex.Matches(input))
        {
            string tag = match.Groups[2].Value.ToLower();
            bool isClosing = match.Groups[1].Value == "/";

            if (!allowedTags.Contains(tag))
            {
                errorMessage = $"BBCode tag [{tag}] is not allowed.";
                return false;
            }

            if (isClosing)
            {
                if (stack.Count == 0 || stack.Peek() != tag)
                {
                    errorMessage = $"Closing BBCode tag [/{tag}] is incorrect or not matching.";
                    return false;
                }
                stack.Pop();
            }
            else
            {
                stack.Push(tag);
            }
        }

        if (stack.Count > 0)
        {
            errorMessage = $"Unclosed BBCode tag [{stack.Peek()}] found.";
            return false;
        }

        errorMessage = "";
        return true;
    }
}
