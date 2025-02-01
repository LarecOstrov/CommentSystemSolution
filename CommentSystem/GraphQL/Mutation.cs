using CommentSystem.Models.Inputs;
using CommentSystem.Services.Interfaces;
using Serilog;
using System.Threading.Tasks;

namespace CommentSystem.GraphQL
{
    public class Mutation
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
            catch (GraphQLException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while adding comment");
                throw new GraphQLException("Internal server error");
            }
        }
    }
}
