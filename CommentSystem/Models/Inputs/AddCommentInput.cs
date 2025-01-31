namespace CommentSystem.Models.Inputs
{
    public record AddCommentInput(string UserName, string Email, string? HomePage, string Text, string CaptchaKey, string Captcha, string? ImageUrl, string? TextUrl);
}
