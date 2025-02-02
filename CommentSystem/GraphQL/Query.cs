using Common.Models;
using Common.Services.Interfaces;
using Serilog;

namespace Common.GraphQL;

internal class Query
{
    internal readonly ICommentService _commentService;

    public Query(ICommentService commentService)
    {
        _commentService = commentService;
    }

    public async Task<List<Comment>> GetComments(string? sortBy = null, bool descending = true, int page = 1, int pageSize = 25)
    {
        try
        {
            return await _commentService.GetAllCommentsWithSortingAndPaginationAsync(sortBy, descending, page, pageSize);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error while getting comments: sortBy - {sortBy}, descending - {descending}, page - {page}, pageSize - {pageSize}");
            return new List<Comment>();
        }
    }
}
