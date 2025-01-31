using CommentSystem.Models.Inputs;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CommentSystem.Models.DTOs
{
    public class CommentDto
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Email { get; set; }
        public string? HomePage { get; set; }
        [Required]
        public string Text { get; set; }
        public string? ImageUrl { get; set; }
        public string? TextUrl { get; set; }

        public static CommentDto FromAddCommentInput(AddCommentInput input)
        {
            return new CommentDto
            {
                UserName = input.UserName,
                Email = input.Email,
                HomePage = input.HomePage,
                Text = input.Text,
                ImageUrl = input.ImageUrl,
                TextUrl = input.TextUrl
            };
        }
    }
}

