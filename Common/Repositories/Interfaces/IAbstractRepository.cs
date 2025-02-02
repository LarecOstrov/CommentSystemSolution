namespace Common.Repositories.Interfaces;

public interface IAbstractRepository<T> where T : class
{
    IQueryable<T> GetAll();
    Task<T?> GetByIdAsync(Guid id);
    Task<bool> AddAsync(T comment);
    Task<bool> UpdateAsync(T comment);
    Task<bool> DeleteAsync(Guid id);
}