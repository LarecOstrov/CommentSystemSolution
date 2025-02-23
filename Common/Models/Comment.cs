using HotChocolate;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Common.Models
{
    public class Comment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public required string Text { get; set; }

        public Guid? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public Comment? Parent { get; set; }
        public List<Comment> Replies { get; set; } = new();

        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<FileAttachment>? FileAttachments { get; set; } = null;

        public bool HasReplies { get; set; } = false;
    }
}
