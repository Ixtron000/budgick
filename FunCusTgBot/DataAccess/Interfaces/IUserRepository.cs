using DataAccess.Entities;

namespace DataAccess.Interfaces
{
    public interface IUserRepository : IBaseRepository<UserEntity>
    {
        Task<bool> IsAdminAsync(long userId);
    }
}
