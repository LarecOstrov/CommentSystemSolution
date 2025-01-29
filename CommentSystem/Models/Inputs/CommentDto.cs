namespace CommentSystem.Models.Inputs
{
    public record CommentDto(string UserName, string Email, string? HomePage, string Text, string Captcha);
}

