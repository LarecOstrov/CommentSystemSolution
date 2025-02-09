using Common.Models;
using Common.Repositories.Interfaces;
using Common.Services.Interfaces;
using Serilog;

namespace Common.Services.Implementations;

public class FileAttachmentService : IFileAttachmentService
{
    private readonly IFileAttachmentRepository _fileRepository;

    public FileAttachmentService(IFileAttachmentRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task<FileAttachment?> GetFileByIdAsync(Guid fileId)
    {
        return await _fileRepository.GetByIdAsync(fileId);
    }

    public async Task<List<FileAttachment>> GetFilesByCommentIdAsync(Guid commentId)
    {
        return await _fileRepository.GetByCommentIdAsync(commentId);
    }

    public async Task<FileAttachment> AddFileAsync(FileAttachment fileAttachment)
    {
        try
        {
            return await _fileRepository.AddAsync(fileAttachment);
        }
        catch (Exception ex)
        {
            Log.Error($"Error while adding file record: {ex.Message}");
            throw;
        }
    }

    public async Task<List<FileAttachment>> AddManyFileAsync(List<FileAttachment> fileAttachments)
    {
        try
        {
            return await _fileRepository.AddManyAsync(fileAttachments);
        }
        catch (Exception ex)
        {
            Log.Error($"Error while adding file record: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(Guid fileId)
    {
        try
        {
            return await _fileRepository.DeleteAsync(fileId);
        }
        catch (Exception ex)
        {
            Log.Error($"Error while removing file record: {ex.Message}");
            return false;
        }
    }
}
