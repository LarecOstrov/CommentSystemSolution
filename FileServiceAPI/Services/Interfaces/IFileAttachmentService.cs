using Common.Models;

namespace FileServiceAPI.Services.Interfaces;

internal interface IFileAttachmentService
{
    Task<FileAttachment?> GetFileByIdAsync(Guid fileId);
    Task<List<FileAttachment>> GetFilesByCommentIdAsync(Guid commentId);
    Task<bool> AddFileAsync(FileAttachment fileAttachment);
    Task<bool> DeleteFileAsync(Guid fileId);
}
