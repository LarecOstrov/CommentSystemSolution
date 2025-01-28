using CommentSystem.Models;

namespace CommentSystem.Repositories.Interfaces
{
    public interface ICommentRepository
    {
        IQueryable<Comment> GetAll();
        Task<Comment?> GetByIdAsync(Guid id);
        Task AddAsync(Comment comment);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(Guid id);
    }
}