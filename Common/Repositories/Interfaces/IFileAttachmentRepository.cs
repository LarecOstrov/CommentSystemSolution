using Common.Models;

namespace Common.Repositories.Interfaces;

public interface IFileAttachmentRepository : IAbstractRepository<FileAttachment>
{
    Task<List<FileAttachment>> AddManyAsync(List<FileAttachment> comments);
    Task<List<FileAttachment>> GetOrphanFilesAsync(TimeSpan olderThan);
    Task<List<FileAttachment>> GetByCommentIdAsync(Guid commentId);
    Task<bool> DeleteManyAsync(IEnumerable<FileAttachment> files);
}