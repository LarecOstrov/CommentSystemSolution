using Common.Models;
using Common.Repositories.Interfaces;
using Serilog;

namespace Common.GraphQL;

public class Query
{
    private readonly ICommentRepository _commentRepository;

    public Query(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    [UsePaging (IncludeTotalCount = true, MaxPageSize = 25)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Comment> GetComments()
    {
        try
        {
            //TODO: add cache
            return _commentRepository.GetAll();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            throw new GraphQLException($"Failed to get comments {ex.Message}");
        }
    }
}
