using Common.Data;
using Common.Models;
using Common.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Common.Repositories.Implementations;

public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<Comment> GetAll()
    {
        return _context.Comments
            .Include(c => c.Replies)
            .Include(c => c.User)
            .AsQueryable();
    }

    public async Task<Comment?> GetByIdAsync(Guid id)
    {
        return await _context.Comments
            .Include(c => c.Replies)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Comment> AddAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
        if (await _context.SaveChangesAsync() > 0)
        {
            return comment;
        }
        throw new DbUpdateException("Failed to add comment");
    }

    public async Task<Comment> UpdateAsync(Comment comment)
    {
        _context.Comments.Update(comment);
        if(await _context.SaveChangesAsync() > 0)
        {
            return comment;
        }
        throw new DbUpdateException("Failed to update comment");
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            return await _context.SaveChangesAsync() > 0;
        }
        return false;
    }

    public async Task<bool> UpdateHasAttachmentAsync(Guid id, bool hasAttachment)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment is not null)
        {
            comment.HasAttachment = hasAttachment;
            _context.Comments.Update(comment);
            return await _context.SaveChangesAsync() > 0;
        }
        return false;

    }
}
