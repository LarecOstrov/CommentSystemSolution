namespace Common.Models.Inputs;

public record AddCommentInput(string UserName, string Email, string? HomePage, string Text, Guid CaptchaKey, string Captcha, bool HasAttachment = false);
