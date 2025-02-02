using Common.Models.Inputs;

namespace Common.Models.DTOs;

public class CommentDto
{
    public required Guid Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string? HomePage { get; set; }
    public required string Text { get; set; }
    public required bool HasAttachment { get; set; } = false;

    public static CommentDto FromAddCommentInput(AddCommentInput input)
    {
        return new CommentDto
        {
            Id = input.CaptchaKey,
            UserName = input.UserName,
            Email = input.Email,
            HomePage = input.HomePage,
            Text = input.Text,
            HasAttachment = input.HasAttachment
        };
    }
}

