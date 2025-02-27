using Common.Models;

namespace Common.Repositories.Interfaces
{
    public interface IUserRepository : IAbstractRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
