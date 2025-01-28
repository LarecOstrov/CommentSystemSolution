using CommentSystem.GraphQL.Inputs;
using CommentSystem.Models;

namespace CommentSystem.Services.Interfaces
{
    public interface ICommentService
    {
        Task<List<Comment>> GetAllCommentsWithSortingAndPaginationAsync(string? sortBy, bool descending, int page, int pageSize);
        Task<Comment> AddCommentAsync(AddCommentInput input);        
    }
}
