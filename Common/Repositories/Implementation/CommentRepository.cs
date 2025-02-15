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
            .Include(c => c.User)
            .Include(c => c.FileAttachments)
            .AsQueryable();
    }

    public async Task<Comment?> GetByIdAsync(Guid id)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Include(c => c.FileAttachments)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Comment> AddAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
        if (await _context.SaveChangesAsync() > 0)
        {
            if (comment.ParentId.HasValue)
            {
                var parentComment = await _context.Comments.FindAsync(comment.ParentId.Value);
                if (parentComment is not null && !parentComment.HasReplies)
                {
                    parentComment.HasReplies = true;
                    await _context.SaveChangesAsync();
                }
            }
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
            var parentId = comment.ParentId;
            _context.Comments.Remove(comment);
            if (await _context.SaveChangesAsync() > 0)
            {
                if (parentId.HasValue)
                {
                    bool hasOtherReplies = await _context.Comments.AnyAsync(c => c.ParentId == parentId.Value);
                    if (!hasOtherReplies)
                    {
                        var parentComment = await _context.Comments.FindAsync(parentId.Value);
                        if (parentComment is not null)
                        {
                            parentComment.HasReplies = false;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                return true;
            };
        }
        return false;
    }    
}
