using AngleSharp.Io.Dom;

namespace Common.Models.Inputs;

public record CommentInput(string UserName, string Email, string? HomePage, string Text, Guid? ParentId, Guid CaptchaKey, string Captcha);
