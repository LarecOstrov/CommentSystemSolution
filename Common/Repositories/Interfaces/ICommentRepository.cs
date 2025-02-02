using Common.Models;

namespace Common.Repositories.Interfaces;

public interface ICommentRepository : IAbstractRepository<Comment>
{
    Task<bool> UpdateHasAttachmentAsync(Guid id, bool hasAttachment);
}