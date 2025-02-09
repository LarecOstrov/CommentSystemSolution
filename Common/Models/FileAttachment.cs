using Common.Enums;

namespace Common.Models;
public class FileAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid CommentId { get; set; }
    public required Comment Comment { get; set; } = null!;
    public required string Url { get; set; }
    public required FileType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
