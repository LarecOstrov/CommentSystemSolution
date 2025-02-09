using Common.Models.Inputs;

namespace Common.Models.DTOs;

public class CommentDto
{
    public required Guid Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string? HomePage { get; set; }
    public required string Text { get; set; }
    public Guid? ParentId { get; set; } = null;
    public List<string>? FileAttachmentUrls { get; set; } = null;

    public static CommentDto FromCommentInput(CommentInput input, List<string>? fileAttachmentUrls = null)
    {
        return new CommentDto
        {
            Id = input.CaptchaKey,
            UserName = input.UserName,
            Email = input.Email,
            HomePage = input.HomePage,
            Text = input.Text,
            ParentId = input.ParentId,
            FileAttachmentUrls = fileAttachmentUrls
        };
    }
}

