using CommentSystem.Models;
using CommentSystem.Models.DTOs;
using CommentSystem.Models.Inputs;

namespace CommentSystem.Services.Interfaces
{
    public interface ICommentService
    {
        Task<List<Comment>> GetAllCommentsWithSortingAndPaginationAsync(string? sortBy, bool descending, int page, int pageSize);
        Task AddCommentAsync(CommentDto input);
        Task PublishCommentAsync(AddCommentInput input);
    }
}
