using Common.Models;
using Common.Models.Inputs;
using Microsoft.AspNetCore.Http;

namespace Common.Services.Interfaces;

public interface ICommentService
{
    Task<List<Comment>> GetAllCommentsWithSortingAndPaginationAsync(string? sortBy, bool descending, int page, int pageSize);
    Task ProcessingCommentAsync(CommentInput input, List<IFormFile>? fileAttachments);
}
