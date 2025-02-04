using Common.Models.Inputs;
using Common.Services.Interfaces;
using Serilog;

namespace Common.GraphQL;

internal class Mutation
{
    private readonly ICommentService _commentService;

    public Mutation(ICommentService commentService)
    {
        _commentService = commentService;
    }

    public async Task<string> AddComment(AddCommentInput input)
    {
        try
        {
            await _commentService.PublishCommentAsync(input);
            return "Comment is being processed";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while adding comment");
            throw ex is GraphQLException
                ? ex
                : new GraphQLException("Internal server error");
        }
    }
}
