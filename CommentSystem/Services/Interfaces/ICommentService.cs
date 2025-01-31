using CommentSystem.Models;
using CommentSystem.Models.DTOs;

namespace CommentSystem.Services.Interfaces
{
    public interface ICommentService
    {
        Task<List<Comment>> GetAllCommentsWithSortingAndPaginationAsync(string? sortBy, bool descending, int page, int pageSize);
        Task AddCommentAsync(CommentDto input);        
    }
}
