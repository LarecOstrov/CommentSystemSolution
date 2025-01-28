using CommentSystem.Models;
using CommentSystem.Data;
using CommentSystem.Services.Interfaces;
using CommentSystem.GraphQL.Inputs;

namespace CommentSystem.GraphQL
{
    public class Mutation
    {
        private readonly ICommentService _commentService;

        public Mutation(ICommentService commentService)
        {
            _commentService = commentService;
        }

        
        public async Task<Comment> AddComment(AddCommentInput input)
        {
            return await _commentService.AddCommentAsync(input);
        }        
    }
}
