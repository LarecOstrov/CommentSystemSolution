namespace CommentSystem.GraphQL.Inputs
{
    public record AddCommentInput(string UserName, string Email, string? HomePage, string Text, string Captcha);
}