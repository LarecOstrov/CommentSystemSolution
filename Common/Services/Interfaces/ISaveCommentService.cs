using Common.Models;
using Common.Models.DTOs;

namespace Common.Services.Interfaces;

public interface ISaveCommentService
{
    Task<Comment> AddCommentAsync(CommentDto input);
}
