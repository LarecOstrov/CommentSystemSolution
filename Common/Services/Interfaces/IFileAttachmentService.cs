using Common.Models;

namespace Common.Services.Interfaces;

public interface IFileAttachmentService
{
    Task<List<FileAttachment>> AddManyFileAsync(List<FileAttachment> fileAttachments);
    Task<FileAttachment?> GetFileByIdAsync(Guid fileId);
    Task<List<FileAttachment>> GetFilesByCommentIdAsync(Guid commentId);
    Task<FileAttachment> AddFileAsync(FileAttachment fileAttachment);
    Task<bool> DeleteFileAsync(Guid fileId);
}
