using CommentSystem.Models;
using CommentSystem.Data;
using CommentSystem.Services.Interfaces;

namespace CommentSystem.GraphQL
{
    public class Query
    {
        private readonly ICommentService _commentService;

        public Query(ICommentService commentService)
        {
            _commentService = commentService;
        }
        
        public async Task<List<Comment>> GetComments(
            string? sortBy = null,
            bool descending = true,
            int page = 1,
            int pageSize = 25)
        {
            return await _commentService.GetAllCommentsWithSortingAndPaginationAsync(sortBy, descending, page, pageSize);
        }        
    }
}
