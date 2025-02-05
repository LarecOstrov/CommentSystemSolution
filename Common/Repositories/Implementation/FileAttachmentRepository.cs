using Common.Data;
using Common.Models;
using Common.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Common.Repositories.Implementations;

public class FileAttachmentRepository : IFileAttachmentRepository
{
    private readonly ApplicationDbContext _context;

    public FileAttachmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<FileAttachment> GetAll()
    {
        return _context.Files
            .AsQueryable();
    }

    public async Task<List<FileAttachment>> GetByCommentIdAsync(Guid commentId)
    {
        return await _context.Files
            .Where(c => c.CommentId == commentId)
            .ToListAsync();
    }


    public async Task<FileAttachment?> GetByIdAsync(Guid id)
    {
        return await _context.Files
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<FileAttachment> AddAsync(FileAttachment comment)
    {
        await _context.Files.AddAsync(comment);
        if( await _context.SaveChangesAsync() > 0)
        {
            return comment;
        }
        throw new Exception("Failed to save file attachment");
    }

    public async Task<FileAttachment> UpdateAsync(FileAttachment comment)
    {
        _context.Files.Update(comment);
        if( await _context.SaveChangesAsync() > 0)
        {
            return comment;
        }
        throw new Exception("Failed to update file attachment");
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var comment = await _context.Files.FindAsync(id);
        if (comment != null)
        {
            _context.Files.Remove(comment);
            return await _context.SaveChangesAsync() > 0;
        }
        return false;
    }

    public async Task<List<FileAttachment>> GetOrphanFilesAsync(TimeSpan olderThan)
    {
        var thresholdTime = DateTime.UtcNow - olderThan;

        return await _context.Files
    .Where(f => f.CreatedAt < thresholdTime &&
                !_context.Comments.Any(c => c.Id == f.CommentId))
    .ToListAsync();
    }

    public async Task<bool> DeleteManyAsync(IEnumerable<FileAttachment> files)
    {
        _context.Files.RemoveRange(files);
        return await _context.SaveChangesAsync() > 0;
    }
}
