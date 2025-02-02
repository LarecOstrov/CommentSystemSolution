using Common.Models;
using Common.Models.DTOs;
using Common.Models.Inputs;

namespace Common.Services.Interfaces;

internal interface ICommentService
{
    Task<List<Comment>> GetAllCommentsWithSortingAndPaginationAsync(string? sortBy, bool descending, int page, int pageSize);
    Task AddCommentAsync(CommentDto input);
    Task PublishCommentAsync(AddCommentInput input);
    Task UpdateHasAttachmentAsync(Guid id, bool hasAttachment = false);
}
