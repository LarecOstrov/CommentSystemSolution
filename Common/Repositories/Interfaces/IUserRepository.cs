using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Repositories.Interfaces
{
    public interface IUserRepository:IAbstractRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
