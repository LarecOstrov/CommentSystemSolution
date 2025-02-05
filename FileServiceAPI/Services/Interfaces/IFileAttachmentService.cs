using Common.Models;

namespace FileServiceAPI.Services.Interfaces;

public interface IFileAttachmentService
{
    Task<FileAttachment?> GetFileByIdAsync(Guid fileId);
    Task<List<FileAttachment>> GetFilesByCommentIdAsync(Guid commentId);
    Task<FileAttachment> AddFileAsync(FileAttachment fileAttachment);
    Task<bool> DeleteFileAsync(Guid fileId);
}
